import { useState, type FormEvent } from 'react'
import { sendOrder, type OrderResponse } from './api'
import { SYMBOLS, toPayload, validate, type FormErrors, type OrderForm } from './validation'

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

const emptyForm: OrderForm = { ativo: 'PETR4', lado: 'C', quantidade: '', preco: '' }

export default function App() {
  const [form, setForm] = useState(emptyForm)
  const [errors, setErrors] = useState<FormErrors>({})
  const [result, setResult] = useState<OrderResponse | null>(null)
  const [networkError, setNetworkError] = useState(false)
  const [sending, setSending] = useState(false)

  function update(field: keyof OrderForm, value: string) {
    setForm({ ...form, [field]: value })
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()

    const found = validate(form)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setSending(true)
    setNetworkError(false)
    try {
      setResult(await sendOrder(toPayload(form)))
    } catch {
      setResult(null)
      setNetworkError(true)
    } finally {
      setSending(false)
    }
  }

  return (
    <main>
      <h1>OrderGenerator</h1>

      <form onSubmit={handleSubmit} noValidate>
        <label>
          Ativo
          <select value={form.ativo} onChange={(e) => update('ativo', e.target.value)}>
            {SYMBOLS.map((symbol) => (
              <option key={symbol}>{symbol}</option>
            ))}
          </select>
        </label>

        <label>
          Lado
          <select value={form.lado} onChange={(e) => update('lado', e.target.value)}>
            <option value="C">Compra</option>
            <option value="V">Venda</option>
          </select>
        </label>

        <label>
          Quantidade
          <input
            inputMode="numeric"
            value={form.quantidade}
            onChange={(e) => update('quantidade', e.target.value)}
          />
          {errors.quantidade && <span className="error">{errors.quantidade}</span>}
        </label>

        <label>
          Preço
          <input
            inputMode="decimal"
            value={form.preco}
            onChange={(e) => update('preco', e.target.value)}
          />
          {errors.preco && <span className="error">{errors.preco}</span>}
        </label>

        <button type="submit" disabled={sending}>
          {sending ? 'Enviando...' : 'Enviar ordem'}
        </button>
      </form>

      <section aria-live="polite">
        <h2>Resultado</h2>

        {networkError && <p className="error">Não foi possível falar com a API. Verifique se ela está no ar.</p>}

        {result && (
          <>
            <p className={result.sucesso ? 'ok' : 'error'}>{result.sucesso ? 'Sucesso' : 'Erro'}</p>
            <p>Exposição atual: {brl.format(result.exposicao_atual)}</p>
            {result.msg_erro && <p>{result.msg_erro}</p>}
            <pre>{JSON.stringify(result, null, 2)}</pre>
          </>
        )}

        {!result && !networkError && <p>Nenhuma ordem enviada ainda.</p>}
      </section>
    </main>
  )
}
