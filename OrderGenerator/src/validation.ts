export const SYMBOLS = ['PETR4', 'VALE3', 'VIIA4'] as const

export type OrderForm = {
  ativo: string
  lado: 'C' | 'V'
  quantidade: string
  preco: string
}

export type OrderPayload = {
  ativo: string
  lado: 'C' | 'V'
  quantidade: number
  preco: number
}

export type FormErrors = { quantidade?: string; preco?: string }

const MAX_QUANTITY = 99_999
const MAX_PRICE_CENTS = 99_999

function priceInCents(text: string): number | null {
  const match = /^(\d{1,3})(?:[.,](\d{1,2}))?$/.exec(text.trim())
  if (!match) return null

  const cents = Number(match[2]?.padEnd(2, '0') ?? '0')
  return Number(match[1]) * 100 + cents
}

export function validate(form: OrderForm): FormErrors {
  const errors: FormErrors = {}

  const quantity = /^\d+$/.test(form.quantidade.trim()) ? Number(form.quantidade) : NaN
  if (!(quantity >= 1 && quantity <= MAX_QUANTITY)) {
    errors.quantidade = 'A quantidade deve ser um inteiro positivo menor que 100.000.'
  }

  const cents = priceInCents(form.preco)
  if (cents === null || cents < 1 || cents > MAX_PRICE_CENTS) {
    errors.preco = 'O preço deve ser positivo, múltiplo de 0,01 e menor que 1.000,00.'
  }

  return errors
}

export function toPayload(form: OrderForm): OrderPayload {
  return {
    ativo: form.ativo,
    lado: form.lado,
    quantidade: Number(form.quantidade),
    preco: priceInCents(form.preco)! / 100,
  }
}
