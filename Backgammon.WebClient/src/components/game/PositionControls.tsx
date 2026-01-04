import { useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { useToast } from '@/hooks/use-toast'
import { FileDown, FileUp, Copy, Check } from 'lucide-react'

export const PositionControls: React.FC = () => {
  const { invoke } = useSignalR()
  const { toast } = useToast()
  const [showExportModal, setShowExportModal] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)
  const [sgfText, setSgfText] = useState('')
  const [copied, setCopied] = useState(false)

  const handleExport = async () => {
    try {
      const sgf = (await invoke(HubMethods.ExportPosition)) as string
      setSgfText(sgf)
      setShowExportModal(true)
    } catch (error) {
      console.error('Export failed:', error)
      toast({
        title: 'Error',
        description: 'Failed to export position',
        variant: 'destructive',
      })
    }
  }

  const handleImport = async () => {
    try {
      await invoke(HubMethods.ImportPosition, sgfText.trim())
      setShowImportModal(false)
      setSgfText('')
      toast({ title: 'Success', description: 'Position imported' })
    } catch (error) {
      console.error('Import failed:', error)
      toast({
        title: 'Error',
        description: 'Failed to import position. Check SGF format.',
        variant: 'destructive',
      })
    }
  }

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(sgfText)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to copy to clipboard',
        variant: 'destructive',
      })
    }
  }

  return (
    <>
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-medium">Position (SGF)</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-2">
          <Button variant="outline" size="sm" onClick={handleExport}>
            <FileDown className="h-4 w-4 mr-2" />
            Export
          </Button>
          <Button variant="outline" size="sm" onClick={() => setShowImportModal(true)}>
            <FileUp className="h-4 w-4 mr-2" />
            Import
          </Button>
        </CardContent>
      </Card>

      {/* Export Modal */}
      <Dialog open={showExportModal} onOpenChange={setShowExportModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Export Position (SGF Format)</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <Textarea
              value={sgfText}
              readOnly
              rows={6}
              className="font-mono text-xs"
              onClick={(e: React.MouseEvent<HTMLTextAreaElement>) => e.currentTarget.select()}
            />
            <Button onClick={copyToClipboard} className="w-full">
              {copied ? (
                <>
                  <Check className="h-4 w-4 mr-2" />
                  Copied!
                </>
              ) : (
                <>
                  <Copy className="h-4 w-4 mr-2" />
                  Copy to Clipboard
                </>
              )}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* Import Modal */}
      <Dialog open={showImportModal} onOpenChange={setShowImportModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Import Position (SGF Format)</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <Textarea
              value={sgfText}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setSgfText(e.target.value)}
              placeholder="Paste SGF here... Example: (;GM[6]AW[...]AB[...]PL[W])"
              rows={6}
              className="font-mono text-xs"
            />
            <Button onClick={handleImport} disabled={!sgfText.trim()} className="w-full">
              Import Position
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
