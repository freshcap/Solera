interface TotalCardProps {
  total: number
  isLoading?: boolean
}

function TotalCard({ total, isLoading = false }: TotalCardProps) {
  if (isLoading) {
    return (
      <div className="total-card">
        <div className="total-card-content">
          <p className="total-card-label">Total Vehicles</p>
          <p className="total-card-value">Loading...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="total-card">
      <div className="total-card-content">
        <p className="total-card-label">Total Vehicles</p>
        <p className="total-card-value">{total.toLocaleString()}</p>
      </div>
    </div>
  )
}

export default TotalCard

