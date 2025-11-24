import { useState } from 'react'
import './App.css'
import PromptTester from './components/PromptTester'
import ChartView from './components/ChartView'

const pages = [
  { id: 'chart', label: 'Chart', element: <ChartView /> },
  { id: 'claude', label: 'Claude', element: <PromptTester /> },
] as const

type PageId = (typeof pages)[number]['id']

function App() {
  const [activePage, setActivePage] = useState<PageId>('claude')

  return (
    <div className="app-page">
      <nav className="app-navbar">
        <div className="nav-tabs">
          {pages.map((page) => (
            <button
              key={page.id}
              type="button"
              className={page.id === activePage ? 'active' : ''}
              onClick={() => setActivePage(page.id)}
            >
              {page.label}
            </button>
          ))}
        </div>
      </nav>
      <main>{pages.find((page) => page.id === activePage)?.element}</main>
    </div>
  )
}

export default App
