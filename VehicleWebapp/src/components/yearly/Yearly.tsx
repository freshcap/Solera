import { useState, useEffect, useMemo } from 'react'
import type { VehicleDataYearly } from '../../data/VehicleData'
import PowerTrainFilter from './PowerTrainFilter'
import TotalCard from './TotalCard'
import YearlyLineChart from './YearlyLineChart'

const API_URL = 'http://localhost:5210/vehicles/aggregation/year'

function Yearly() {
  const [data, setData] = useState<VehicleDataYearly[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string>('')
  const [selectedFuel, setSelectedFuel] = useState<string | null>(null)

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

        const jsonData: VehicleDataYearly[] = await response.json()
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

  // Filter data based on selected fuel type
  const filteredData = useMemo(() => {
    if (!selectedFuel) return data
    return data.filter((item) => item.fuel === selectedFuel)
  }, [data, selectedFuel])

  // Calculate total from filtered data
  const total = useMemo(() => {
    return filteredData.reduce((sum, item) => sum + item.totalValue, 0)
  }, [filteredData])

  const handleFilterChange = (fuel: string | null) => {
    setSelectedFuel(fuel)
  }

  if (isLoading) {
    return (
      <div className="yearly-view">
        <header>
          <h1>Yearly Vehicle Data</h1>
        </header>
        <p className="status status-info">Loading vehicle data...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="yearly-view">
        <header>
          <h1>Yearly Vehicle Data</h1>
        </header>
        <p className="status status-error">Error: {error}</p>
      </div>
    )
  }

  return (
    <div className="yearly-view">
      <header>
        <h1>Yearly Vehicle Data</h1>
        <p className="subtitle">
          View yearly vehicle totals by make with power train filtering
        </p>
      </header>

      <div className="yearly-controls">
        <PowerTrainFilter data={data} onFilterChange={handleFilterChange} />
      </div>

      <div className="yearly-content">
        <TotalCard total={total} isLoading={isLoading} />
        <YearlyLineChart data={filteredData} />
      </div>
    </div>
  )
}

export default Yearly

