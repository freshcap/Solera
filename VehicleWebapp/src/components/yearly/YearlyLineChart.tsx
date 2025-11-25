import { useMemo } from 'react'
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
import type { VehicleDataYearly } from '../../data/VehicleData'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
)

interface YearlyLineChartProps {
  data: VehicleDataYearly[]
}

function YearlyLineChart({ data }: YearlyLineChartProps) {
  const chartData = useMemo(() => {
    if (data.length === 0) return null

    // Data is already filtered by the parent component
    const filteredData = data

    // Get unique years and makes
    const years = Array.from(new Set(filteredData.map((item) => item.year))).sort()
    const makes = Array.from(new Set(filteredData.map((item) => item.make))).sort()

    // Aggregate totals by make and year
    const makeYearTotals: { [make: string]: { [year: number]: number } } = {}

    filteredData.forEach((item) => {
      if (!makeYearTotals[item.make]) {
        makeYearTotals[item.make] = {}
      }
      if (!makeYearTotals[item.make][item.year]) {
        makeYearTotals[item.make][item.year] = 0
      }
      makeYearTotals[item.make][item.year] += item.totalValue
    })

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
      '#f97316',
      '#14b8a6',
      '#a855f7',
      '#eab308',
      '#22c55e',
      '#3b82f6',
      '#f43f5e',
      '#0ea5e9',
    ]

    const datasets = makes.map((make, index) => {
      const values = years.map((year) => makeYearTotals[make][year] || 0)
      return {
        label: make,
        data: values,
        borderColor: colors[index % colors.length],
        backgroundColor: `${colors[index % colors.length]}40`,
        tension: 0.4,
        fill: false,
      }
    })

    return {
      labels: years.map((year) => year.toString()),
      datasets,
    }
  }, [data])

  if (!chartData) {
    return (
      <div className="chart-container">
        <p>No data available</p>
      </div>
    )
  }

  return (
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
              text: 'Yearly Vehicle Totals by Make',
            },
            tooltip: {
              mode: 'index',
              intersect: false,
            },
          },
          scales: {
            y: {
              beginAtZero: true,
              ticks: {
                callback: function (value) {
                  return typeof value === 'number' ? value.toLocaleString() : value
                },
              },
            },
          },
          interaction: {
            mode: 'nearest',
            axis: 'x',
            intersect: false,
          },
        }}
      />
    </div>
  )
}

export default YearlyLineChart

