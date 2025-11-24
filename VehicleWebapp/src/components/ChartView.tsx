import { useState, useEffect, useMemo } from 'react'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js'
import { Line } from 'react-chartjs-2'
import type { VehicleData } from '../data/VehicleData'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
)

const API_URL = 'http://localhost:5210/test/data'

interface YearlyTotals {
  [make: string]: {
    [year: number]: number
  }
}

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

    // Aggregate totals by make and year
    const totals: YearlyTotals = {}
    const allYears = new Set<number>()

    data.forEach((vehicle) => {
      if (!totals[vehicle.make]) {
        totals[vehicle.make] = {}
      }

      // Extract all quarterly fields and sum by year
      Object.keys(vehicle).forEach((key) => {
        if (key.startsWith('q') && typeof vehicle[key as keyof VehicleData] === 'number') {
          // Extract year from field name (e.g., "q22025" -> 2025)
          const match = key.match(/q\d+(\d{4})/)
          if (match) {
            const year = parseInt(match[1], 10)
            allYears.add(year)
            const value = vehicle[key as keyof VehicleData] as number

            if (!totals[vehicle.make][year]) {
              totals[vehicle.make][year] = 0
            }
            totals[vehicle.make][year] += value
          }
        }
      })
    })

    // Sort years
    const sortedYears = Array.from(allYears).sort((a, b) => a - b)

    // Get all makes
    const makes = Object.keys(totals).sort()

    // Generate colors for each make
    const colors = [
      '#6366f1',
      '#10b981',
      '#f59e0b',
      '#ef4444',
      '#8b5cf6',
      '#06b6d4',
      '#ec4899',
      '#84cc16',
    ]

    const datasets = makes.map((make, index) => {
      const values = sortedYears.map((year) => totals[make][year] || 0)
      return {
        label: make,
        data: values,
        borderColor: colors[index % colors.length],
        backgroundColor: colors[index % colors.length] + '40',
        tension: 0.1,
      }
    })

    return {
      labels: sortedYears.map((y) => y.toString()),
      datasets,
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
        <h1>Vehicle Totals by Make</h1>
        <p className="subtitle">
          Line chart showing total vehicles by make for each year
        </p>
      </header>

      <div className="chart-container">
        <Line
          data={chartData}
          options={{
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: {
                position: 'top' as const,
              },
              title: {
                display: true,
                text: 'Vehicle Totals by Make Over Time',
              },
            },
            scales: {
              y: {
                beginAtZero: true,
                title: {
                  display: true,
                  text: 'Total Vehicles',
                },
              },
              x: {
                title: {
                  display: true,
                  text: 'Year',
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
