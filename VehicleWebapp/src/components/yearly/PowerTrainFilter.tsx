import { useState, useEffect } from 'react'
import type { VehicleDataYearly } from '../../data/VehicleData'

interface PowerTrainFilterProps {
  data: VehicleDataYearly[]
  onFilterChange: (selectedFuel: string | null) => void
}

function PowerTrainFilter({ data, onFilterChange }: PowerTrainFilterProps) {
  const [selectedFuel, setSelectedFuel] = useState<string | null>(null)

  // Extract unique fuel types from data
  const fuelTypes = Array.from(new Set(data.map((item) => item.fuel))).sort()

  // Notify parent of initial filter state on mount
  useEffect(() => {
    onFilterChange(null)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value
    const newFuel = value === '' ? null : value
    setSelectedFuel(newFuel)
    onFilterChange(newFuel)
  }

  return (
    <div className="filter-container">
      <label htmlFor="powertrain-filter" className="filter-label">
        Power Train
      </label>
      <select
        id="powertrain-filter"
        value={selectedFuel || ''}
        onChange={handleChange}
        className="filter-select"
      >
        <option value="">All</option>
        {fuelTypes.map((fuel) => (
          <option key={fuel} value={fuel}>
            {fuel}
          </option>
        ))}
      </select>
    </div>
  )
}

export default PowerTrainFilter

