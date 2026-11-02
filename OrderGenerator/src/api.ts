import type { OrderPayload } from './validation'

export type OrderResponse = {
  sucesso: boolean
  exposicao_atual: number
  msg_erro: string | null
}

export async function sendOrder(payload: OrderPayload): Promise<OrderResponse> {
  const response = await fetch('/api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })

  return response.json()
}
