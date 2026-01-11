import React from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { BarChart3, TrendingUp, Users, Target } from 'lucide-react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
  ReferenceLine
} from 'recharts'
import type { RatingDistributionDto } from '@/types/players'

interface RatingDistributionProps {
  data: RatingDistributionDto | null
  isLoading: boolean
}

export const RatingDistribution: React.FC<RatingDistributionProps> = ({
  data,
  isLoading
}) => {
  const { user } = useAuth()

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </div>
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (!data || data.totalPlayers === 0) {
    return (
      <div className="text-center py-12">
        <BarChart3 className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <p className="text-muted-foreground">No rating data available</p>
        <p className="text-sm text-muted-foreground mt-1">
          Play some rated games to see the distribution!
        </p>
      </div>
    )
  }

  const chartData = data.buckets.map((bucket) => ({
    name: bucket.label,
    players: bucket.count,
    percentage: bucket.percentage,
    isUserBucket: bucket.isUserBucket,
    minRating: bucket.minRating,
    maxRating: bucket.maxRating
  }))

  return (
    <div className="space-y-6">
      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="p-4 bg-accent rounded-lg">
          <div className="flex items-center gap-2 mb-2">
            <Users className="h-4 w-4 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Total Players</p>
          </div>
          <p className="text-2xl font-bold">{data.totalPlayers.toLocaleString()}</p>
        </div>

        <div className="p-4 bg-accent rounded-lg">
          <div className="flex items-center gap-2 mb-2">
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Average Rating</p>
          </div>
          <p className="text-2xl font-bold">{Math.round(data.averageRating)}</p>
        </div>

        <div className="p-4 bg-accent rounded-lg">
          <div className="flex items-center gap-2 mb-2">
            <Target className="h-4 w-4 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Median Rating</p>
          </div>
          <p className="text-2xl font-bold">{data.medianRating}</p>
        </div>

        {data.userRating && data.userPercentile !== null && data.userPercentile !== undefined && (
          <div className="p-4 bg-primary/10 rounded-lg border border-primary/20">
            <div className="flex items-center gap-2 mb-2">
              <BarChart3 className="h-4 w-4 text-primary" />
              <p className="text-sm text-primary">Your Position</p>
            </div>
            <p className="text-2xl font-bold text-primary">{data.userRating}</p>
            <p className="text-sm text-muted-foreground">
              Top {(100 - data.userPercentile).toFixed(1)}%
            </p>
          </div>
        )}

        {!data.userRating && user && (
          <div className="p-4 bg-muted rounded-lg">
            <div className="flex items-center gap-2 mb-2">
              <BarChart3 className="h-4 w-4 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Your Position</p>
            </div>
            <p className="text-lg font-medium text-muted-foreground">Not ranked yet</p>
            <p className="text-sm text-muted-foreground">Play rated games!</p>
          </div>
        )}
      </div>

      {/* User's percentile badge */}
      {data.userPercentile !== null && data.userPercentile !== undefined && (
        <div className="flex items-center justify-center gap-2 p-4 bg-accent/50 rounded-lg">
          <span className="text-muted-foreground">You are better than</span>
          <Badge variant="default" className="text-lg px-3 py-1">
            {data.userPercentile.toFixed(1)}%
          </Badge>
          <span className="text-muted-foreground">of all players</span>
        </div>
      )}

      {/* Distribution Chart */}
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 60 }}>
            <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
            <XAxis
              dataKey="name"
              angle={-45}
              textAnchor="end"
              height={60}
              tick={{ fontSize: 11 }}
              interval={0}
            />
            <YAxis
              tick={{ fontSize: 12 }}
              label={{
                value: 'Players',
                angle: -90,
                position: 'insideLeft',
                style: { textAnchor: 'middle' }
              }}
            />
            <Tooltip
              content={({ active, payload }) => {
                if (active && payload && payload.length) {
                  const data = payload[0].payload
                  return (
                    <div className="bg-popover border rounded-lg p-3 shadow-lg">
                      <p className="font-semibold">{data.name}</p>
                      <p className="text-sm text-muted-foreground">
                        {data.players} players ({data.percentage}%)
                      </p>
                      {data.isUserBucket && (
                        <Badge variant="default" className="mt-2">
                          Your rating range
                        </Badge>
                      )}
                    </div>
                  )
                }
                return null
              }}
            />
            {data.userRating && (
              <ReferenceLine
                x={chartData.find(b => b.isUserBucket)?.name}
                stroke="hsl(var(--primary))"
                strokeWidth={2}
                strokeDasharray="5 5"
              />
            )}
            <Bar dataKey="players" radius={[4, 4, 0, 0]}>
              {chartData.map((entry, index) => (
                <Cell
                  key={`cell-${index}`}
                  fill={entry.isUserBucket ? 'hsl(var(--primary))' : 'hsl(var(--muted-foreground))'}
                  fillOpacity={entry.isUserBucket ? 1 : 0.5}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      <p className="text-center text-sm text-muted-foreground">
        Rating distribution of all ranked players
      </p>
    </div>
  )
}
