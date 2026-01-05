import React, { useState, useRef, useEffect } from 'react'
import { useGameStore } from '@/stores/gameStore'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, Send } from 'lucide-react'
import { ScrollArea } from '@/components/ui/scroll-area'

export const ChatPanel: React.FC = () => {
  const { chatMessages, toggleChat, showChat } = useGameStore()
  const { invoke } = useSignalR()
  const [message, setMessage] = useState('')
  const scrollAreaRef = useRef<HTMLDivElement>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: 'smooth' })
    }
  }, [chatMessages])

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!message.trim()) return

    try {
      await invoke(HubMethods.SendChatMessage, message)
      setMessage('')
    } catch (error) {
      console.error('Failed to send chat message:', error)
    }
  }

  if (!showChat) return null

  return (
    <Card className="w-full">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">Chat</CardTitle>
        <Button
          variant="ghost"
          size="sm"
          onClick={toggleChat}
          className="h-6 w-6 p-0"
        >
          <X className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        <ScrollArea className="h-48 w-full rounded-md border p-3" ref={scrollAreaRef}>
          <div className="space-y-2">
            {chatMessages.length === 0 ? (
              <div className="text-center text-sm text-muted-foreground py-8">
                No messages yet
              </div>
            ) : (
              chatMessages.map((msg, index) => (
                <div
                  key={index}
                  className={`flex flex-col ${msg.isOwn ? 'items-end' : 'items-start'}`}
                >
                  <div
                    className={`rounded-lg px-3 py-2 max-w-[85%] ${
                      msg.isOwn
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-muted'
                    }`}
                  >
                    <div className="text-xs font-semibold mb-1">
                      {msg.senderName}
                    </div>
                    <div className="text-sm break-words">{msg.message}</div>
                  </div>
                  <div className="text-xs text-muted-foreground mt-1">
                    {msg.timestamp.toLocaleTimeString([], {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </div>
                </div>
              ))
            )}
            <div ref={messagesEndRef} />
          </div>
        </ScrollArea>

        <form onSubmit={handleSendMessage} className="flex gap-2">
          <Input
            type="text"
            placeholder="Type a message..."
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            className="flex-1"
            maxLength={500}
          />
          <Button type="submit" size="sm" disabled={!message.trim()}>
            <Send className="h-4 w-4" />
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
