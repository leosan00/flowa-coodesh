# OrderAccumulator & OrderGenerator

Sistema que recebe ordens de compra e venda, calcula a **exposição financeira por ativo** e rejeita as que ultrapassam o limite de R$ 1.000.000. É composto por:

- **OrderAccumulator** (backend, C# / .NET 10): API REST/JSON que valida as ordens, controla a exposição e publica eventos no Kafka.
- **OrderGenerator** (frontend, React): página com o formulário de ordens, que exibe a resposta da API.

> This is a challenge by [Coodesh](https://coodesh.com/)

## Tecnologias

| Camada | Tecnologias |
|---|---|
| Backend | C#, .NET 10, ASP.NET Core (controllers), Entity Framework Core 10 |
| Banco de dados | Microsoft SQL Server 2022 |
| Mensageria | Apache Kafka 3.9 (KRaft), Confluent.Kafka, Kafbat UI |
| Frontend | React 19, Vite, TypeScript, nginx |
| Testes | xUnit e Moq (backend); Vitest e Testing Library (frontend) |
| Infraestrutura | Docker e Docker Compose |

## Como executar

Pré-requisito: **Docker** (com Docker Compose).

```bash
docker compose up --build
```

Na primeira vez as imagens são baixadas e construídas, o que leva alguns minutos. Quando terminar:

| O quê | Endereço |
|---|---|
| Formulário de ordens (OrderGenerator) | http://localhost:5173 |
| API e Swagger | http://localhost:5080/swagger |
| Kafbat (tópicos e mensagens do Kafka) | http://localhost:8081 |

O banco e o esquema são criados automaticamente (serviço `sqlserver-init`), assim como o tópico `orders` (serviço `kafka-init`). Para parar tudo, `docker compose down` (acrescente `-v` para apagar também os dados).

Se algo já estiver usando as portas 1433, 5080, 5173, 8081 ou 9092, pare o que as ocupa antes de subir.

### Desenvolvimento local

Para rodar a API e o frontend fora do Docker, suba só a infraestrutura:

```bash
docker compose up -d sqlserver sqlserver-init kafka kafka-init kafbat
```

Em outro terminal, a API (http://localhost:5080/swagger):

```bash
dotnet run --project src/Api
```

E o frontend (http://localhost:5173, com o Vite repassando `/api/*` para a API):

```bash
cd OrderGenerator
npm install
npm run dev
```

### Testes

Não precisam de Docker:

```bash
dotnet test OrderAccumulator.slnx
```

```bash
cd OrderGenerator
npm test
```

## Regras de negócio

**Exposição financeira** = soma de (preço × quantidade) das ordens de compra − soma de (preço × quantidade) das ordens de venda, por ativo. Compras aumentam a exposição, vendas diminuem.

- Limite de **R$ 1.000.000 em valor absoluto por ativo**. Uma ordem que faria a exposição ultrapassar o limite é rejeitada e **não é registrada**. Chegar exatamente em R$ 1.000.000 é aceito.
- Ativos: `PETR4`, `VALE3` e `VIIA4`.
- Quantidade: inteiro positivo menor que 100.000.
- Preço: positivo, múltiplo de 0,01 e menor que 1.000.
- Premissa: a quantidade executada é a quantidade total da ordem (a ordem é aceita ou rejeitada por inteiro).

## API

### `POST /orders`

```json
{ "ativo": "PETR4", "lado": "C", "quantidade": 584, "preco": 54.87 }
```

`lado` é `"C"` (compra) ou `"V"` (venda). A resposta tem sempre o mesmo formato:

```json
{ "sucesso": true, "exposicao_atual": 32044.08, "msg_erro": null }
```

| Situação | Status | `sucesso` |
|---|---|---|
| Ordem aceita e registrada | 200 | `true` |
| Requisição ou valores inválidos (ativo, lado, quantidade, preço, JSON) | 400 | `false` |
| Ordem válida que ultrapassaria o limite de exposição | 409 | `false` |

Em caso de erro, `msg_erro` explica o motivo e `exposicao_atual` traz a exposição vigente do ativo, inalterada.

### Consultas

- `GET /exposure`: exposição atual dos três ativos (`[{ "ativo": "PETR4", "exposicao_atual": 32044.08 }, ...]`).
- `GET /orders?take=50`: ordens processadas (aceitas e rejeitadas), da mais recente para a mais antiga. Padrão 50, máximo 500.

## Arquitetura

O backend segue **Clean Architecture** orientada a use cases, com as dependências sempre apontando para dentro:

```
Api ──► Application ──► Domain
 └────► Infrastructure ─┘
```

| Projeto | Responsabilidade |
|---|---|
| `Domain` | Regras de negócio: `AssetExposure` (agregado que garante o limite), `Order`, resultados e eventos de domínio |
| `Application` | Use cases (`PlaceOrder`, `GetExposures`, `GetOrderHistory`) e as interfaces que a Infrastructure implementa. Só orquestra, sem regra de negócio |
| `Infrastructure` | EF Core e SQL Server, repositórios, outbox, relay, publisher e consumer Kafka |
| `Api` | Controllers, contratos JSON, validação do formato da requisição e registro de dependências |

### Registro de uma ordem

1. O use case valida os valores e cria a `Order`.
2. O repositório abre uma transação, lê a linha do ativo com `UPDLOCK`, pede ao Domain que aplique a ordem (`AssetExposure.Apply`) e grava a nova exposição **e** o evento (`OrderAccepted` ou `OrderRejected`) na tabela `Outbox`, na mesma transação.
3. A API responde com o resultado, de forma síncrona.
4. Em segundo plano, o relay lê o `Outbox`, publica no tópico `orders` do Kafka (chave = ativo) e marca a mensagem como processada.
5. Um consumer lê o tópico e monta o histórico de ordens (`GET /orders`).

### Decisões

- **Concorrência.** Duas ordens do mesmo ativo não podem ler a exposição ao mesmo tempo, senão as duas passariam pelo limite. Por isso a linha do ativo é travada (`UPDLOCK`) da leitura até o commit, o que também vale com várias instâncias da API. Verificado com 150 requisições simultâneas de R$ 10.000 no mesmo ativo: 100 aceitas, 50 rejeitadas e exposição final de exatamente R$ 1.000.000.
- **Outbox transacional.** O evento é gravado junto com a exposição, então nunca se perde: se o Kafka estiver fora do ar, a API continua aceitando ordens e o relay publica quando ele voltar.
- **Entrega "pelo menos uma vez" e idempotência.** Se a API cair entre publicar e marcar a mensagem, ela é publicada de novo. O consumer usa o `EventId` como chave primária do histórico e ignora a duplicata.
- **Ordem por ativo.** A chave da mensagem é o ativo, então as ordens do mesmo ativo ficam na mesma partição.
- **Decisão síncrona.** O contrato exige `exposicao_atual` na resposta, por isso quem decide é o banco, na hora. Só os efeitos colaterais (histórico) passam pelo Kafka.

### Limites conhecidos

- O histórico é eventualmente consistente: aparece 1 a 2 segundos depois da ordem. A exposição de `GET /exposure` é imediata.
- Um relay só: com mais de uma instância da API, os relays disputariam o mesmo lote.
- Mensagens inválidas no tópico são registradas e descartadas, sem fila de erros (dead letter).
- Sem autenticação.
- A atomicidade do repositório (`UPDLOCK`, outbox) foi verificada manualmente contra o SQL Server e o Kafka reais, não por testes automatizados. Os testes automatizados cobrem Domain, use cases, contrato HTTP, relay, publisher e projeção do histórico.

## Banco de dados

O esquema está em `database/` (`01-create-database.sql` e `02-schema.sql`), com scripts idempotentes aplicados pelo `sqlserver-init`. O `02-schema.sql` é gerado a partir das migrations do EF Core. Ao criar uma migration nova:

```bash
dotnet ef migrations add NomeDaMigration --project src/Infrastructure --startup-project src/Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/Infrastructure --startup-project src/Infrastructure --output database/02-schema.sql
```

(Remova o BOM do início do arquivo gerado, que atrapalha o `sqlcmd` no Linux.)

As credenciais do SQL Server (usuário `sa`) estão no `docker-compose.yml` e servem **apenas para este ambiente de teste**.

## Estrutura

```
OrderAccumulator.slnx
src/
  Domain/            regras de negócio
  Application/       use cases
  Infrastructure/    banco, outbox e Kafka
  Api/               controllers e Dockerfile
tests/               testes de cada projeto
OrderGenerator/      frontend (React) e Dockerfile
database/            scripts SQL
docker-compose.yml
```
