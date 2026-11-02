import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

function mockApi(body: object) {
  const fetchMock = vi.fn().mockResolvedValue({ json: async () => body })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function fillAndSubmit(quantidade: string, preco: string) {
  fireEvent.change(screen.getByLabelText('Quantidade'), { target: { value: quantidade } })
  fireEvent.change(screen.getByLabelText('Preço'), { target: { value: preco } })
  fireEvent.click(screen.getByRole('button', { name: 'Enviar ordem' }))
}

describe('App', () => {
  it('mostra os erros do formulário e não chama a API', () => {
    const fetchMock = mockApi({})
    render(<App />)

    fillAndSubmit('0', '1000')

    expect(screen.getByText(/quantidade deve ser um inteiro positivo/i)).toBeTruthy()
    expect(screen.getByText(/preço deve ser positivo/i)).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('envia a ordem e mostra o sucesso com a exposição em reais', async () => {
    const fetchMock = mockApi({ sucesso: true, exposicao_atual: 32044.08, msg_erro: null })
    render(<App />)

    fireEvent.change(screen.getByLabelText('Ativo'), { target: { value: 'VALE3' } })
    fireEvent.change(screen.getByLabelText('Lado'), { target: { value: 'V' } })
    fillAndSubmit('584', '54,87')

    expect(await screen.findByText('Sucesso')).toBeTruthy()
    expect(screen.getByText(/32\.044,08/)).toBeTruthy()

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/orders')
    expect(JSON.parse(init.body)).toEqual({ ativo: 'VALE3', lado: 'V', quantidade: 584, preco: 54.87 })
  })

  it('mostra o erro e a mensagem quando a API rejeita a ordem', async () => {
    mockApi({ sucesso: false, exposicao_atual: 999990, msg_erro: 'ultrapassando o limite' })
    render(<App />)

    fillAndSubmit('10', '1,01')

    expect(await screen.findByText('Erro')).toBeTruthy()
    expect(screen.getByText('ultrapassando o limite')).toBeTruthy()
    expect(screen.getByText(/999\.990,00/)).toBeTruthy()
  })

  it('avisa quando não consegue falar com a API', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    render(<App />)

    fillAndSubmit('10', '10')

    expect(await screen.findByText(/não foi possível falar com a api/i)).toBeTruthy()
  })
})
