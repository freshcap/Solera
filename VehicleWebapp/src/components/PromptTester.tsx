import { useState } from 'react'
import type { FormEvent } from 'react'

const API_URL = 'http://localhost:5210/test/claude'

function PromptTester() {
  const [prompt, setPrompt] = useState('')
  const [result, setResult] = useState<string>('')
  const [errorMessage, setErrorMessage] = useState<string>('')
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!prompt.trim()) {
      setErrorMessage('Please enter a prompt before testing.')
      return
    }

    setIsLoading(true)
    setErrorMessage('')
    setResult('')

    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json, text/plain',
        },
        body: JSON.stringify({ prompt }),
      })

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`)
      }

      const contentType = response.headers.get('content-type') ?? ''
      let payload: unknown

      if (contentType.includes('application/json')) {
        payload = await response.json()
      } else {
        payload = await response.text()
      }

      const formatted =
        typeof payload === 'string'
          ? payload
          : JSON.stringify(payload, null, 2)

      setResult(formatted || 'Success (empty response body)')
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Unexpected error occurred.'
      setErrorMessage(message)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="app-shell">
      <header>
        <h1>Prompt Test Console</h1>
        <p className="subtitle">
          Send a prompt to the local API and inspect the response.
        </p>
      </header>

      <form className="prompt-form" onSubmit={handleSubmit}>
        <label htmlFor="prompt">Prompt</label>
        <textarea
          id="prompt"
          name="prompt"
          rows={4}
          placeholder="Type something to send to the API..."
          value={prompt}
          onChange={(event) => setPrompt(event.target.value)}
          disabled={isLoading}
        />
        <div className="form-actions">
          <button type="submit" disabled={isLoading}>
            {isLoading ? 'Testing…' : 'Test'}
          </button>
        </div>
      </form>

      {errorMessage && <p className="status status-error">{errorMessage}</p>}

      <section className="response-panel">
        <div className="panel-header">
          <h2>Response</h2>
          {isLoading && <span className="status status-info">Loading…</span>}
        </div>
        <pre className="response-body">
          {result || 'Submit a prompt to see the API response here.'}
        </pre>
      </section>
    </div>
  )
}

export default PromptTester

