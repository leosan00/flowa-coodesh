import { describe, expect, it } from 'vitest'
import { toPayload, validate, type OrderForm } from './validation'

const form = (overrides: Partial<OrderForm> = {}): OrderForm => ({
  ativo: 'PETR4',
  lado: 'C',
  quantidade: '584',
  preco: '54.87',
  ...overrides,
})

describe('validate', () => {
  it('aceita uma ordem válida', () => {
    expect(validate(form())).toEqual({})
  })

  it.each(['1', '99999'])('aceita a quantidade %s', (quantidade) => {
    expect(validate(form({ quantidade })).quantidade).toBeUndefined()
  })

  it.each(['', '0', '-5', '100000', '10.5', 'abc'])('recusa a quantidade "%s"', (quantidade) => {
    expect(validate(form({ quantidade })).quantidade).toBeDefined()
  })

  it.each(['0.01', '54.87', '54,87', '10', '999.99'])('aceita o preço %s', (preco) => {
    expect(validate(form({ preco })).preco).toBeUndefined()
  })

  it.each(['', '0', '0.00', '1000', '1000.00', '10.015', '-1', 'abc', '1,2,3'])('recusa o preço "%s"', (preco) => {
    expect(validate(form({ preco })).preco).toBeDefined()
  })

  it('aponta os dois campos quando ambos estão inválidos', () => {
    const errors = validate(form({ quantidade: '0', preco: '0' }))

    expect(Object.keys(errors)).toEqual(['quantidade', 'preco'])
  })
})

describe('toPayload', () => {
  it('converte o formulário para o JSON da API', () => {
    expect(toPayload(form({ ativo: 'VALE3', lado: 'V' }))).toEqual({
      ativo: 'VALE3',
      lado: 'V',
      quantidade: 584,
      preco: 54.87,
    })
  })

  it('lê o preço com vírgula sem erro de ponto flutuante', () => {
    expect(toPayload(form({ preco: '0,30' })).preco).toBe(0.3)
    expect(toPayload(form({ preco: '10,5' })).preco).toBe(10.5)
  })
})
