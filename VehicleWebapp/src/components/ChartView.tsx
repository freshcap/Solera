import { useState, useEffect, useMemo } from 'react'
import {
  Chart as ChartJS,
  ArcElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js'
import { Pie } from 'react-chartjs-2'
import type { VehicleData } from '../data/VehicleData'

ChartJS.register(ArcElement, Title, Tooltip, Legend)

const API_URL = 'http://localhost:5210/test/data'

function ChartView() {
  const [data, setData] = useState<VehicleData[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string>('')

  useEffect(() => {
    const fetchData = async () => {
      setIsLoading(true)
      setError('')

      try {
        const response = await fetch(API_URL, {
          method: 'GET',
          headers: {
            Accept: 'application/json',
          },
        })

        if (!response.ok) {
          throw new Error(`Request failed with status ${response.status}`)
        }

        const jsonData: VehicleData[] = await response.json()
        setData(jsonData)
      } catch (err) {
        const message =
          err instanceof Error ? err.message : 'Unexpected error occurred.'
        setError(message)
      } finally {
        setIsLoading(false)
      }
    }

    fetchData()
  }, [])

  const chartData = useMemo(() => {
    if (data.length === 0) return null

    // Aggregate totals by make and model combination
    const totals: { [key: string]: number } = {}

    data.forEach((vehicle) => {
      const label = `${vehicle.make} - ${vehicle.model}`
      if (!totals[label]) {
        totals[label] = 0
      }
      totals[label] += vehicle.total
    })

    // Sort by total (descending) and take top entries for better visualization
    const entries = Object.entries(totals)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 20) // Limit to top 20 for readability

    const labels = entries.map(([label]) => label)
    const values = entries.map(([, total]) => total)

    // Generate colors for each segment
    const colors = [
      '#6366f1',
      '#10b981',
      '#f59e0b',
      '#ef4444',
      '#8b5cf6',
      '#06b6d4',
      '#ec4899',
      '#84cc16',
      '#f97316',
      '#14b8a6',
      '#a855f7',
      '#eab308',
      '#22c55e',
      '#3b82f6',
      '#ec4899',
      '#f43f5e',
      '#0ea5e9',
      '#8b5cf6',
      '#06b6d4',
      '#10b981',
    ]

    return {
      labels,
      datasets: [
        {
          label: 'Total Vehicles',
          data: values,
          backgroundColor: colors.slice(0, labels.length),
          borderColor: '#ffffff',
          borderWidth: 2,
        },
      ],
    }
  }, [data])

  if (isLoading) {
    return (
      <div className="chart-view">
        <p className="status status-info">Loading vehicle data...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="chart-view">
        <p className="status status-error">Error: {error}</p>
      </div>
    )
  }

  if (!chartData) {
    return (
      <div className="chart-view">
        <p>No data available</p>
      </div>
    )
  }

  return (
    <div className="chart-view">
      <header>
        <h1>Vehicle Totals by Make and Model</h1>
        <p className="subtitle">
          Pie chart showing total vehicles by make and model
        </p>
      </header>

      <div className="chart-container">
        <Pie
          data={chartData}
          options={{
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'right' as const,
              },
              title: {
                display: true,
                text: 'Vehicle Distribution by Make and Model',
              },
              tooltip: {
                callbacks: {
                  label: (context) => {
                    const label = context.label || ''
                    const value = context.parsed || 0
                    const total = context.dataset.data.reduce(
                      (a: number, b: number) => a + b,
                      0
                    ) as number
                    const percentage = ((value / total) * 100).toFixed(1)
                    return `${label}: ${value.toLocaleString()} (${percentage}%)`
                  },
                },
              },
            },
          }}
        />
      </div>
    </div>
  )
}

export default ChartView

