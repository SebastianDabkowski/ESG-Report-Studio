import { useEffect, useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Warning, ArrowClockwise } from '@phosphor-icons/react';

interface SessionTimeoutWarningProps {
  /**
   * Whether the warning dialog is shown
   */
  open: boolean;
  
  /**
   * Minutes until session expires
   */
  minutesUntilExpiration?: number;
  
  /**
   * Whether session refresh is allowed
   */
  canRefresh: boolean;
  
  /**
   * Callback to refresh the session
   */
  onRefresh: () => Promise<boolean>;
  
  /**
   * Callback when user dismisses the warning
   */
  onDismiss: () => void;
  
  /**
   * Callback when session expires (optional)
   */
  onExpired?: () => void;
}

/**
 * Dialog component that warns users about imminent session timeout
 */
export function SessionTimeoutWarning({
  open,
  minutesUntilExpiration = 5,
  canRefresh,
  onRefresh,
  onDismiss,
  onExpired
}: SessionTimeoutWarningProps) {
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [timeLeft, setTimeLeft] = useState(minutesUntilExpiration);
  const [error, setError] = useState<string | null>(null);

  // Update countdown timer
  useEffect(() => {
    if (!open) {
      setTimeLeft(minutesUntilExpiration);
      return;
    }

    setTimeLeft(minutesUntilExpiration);
    
    const interval = setInterval(() => {
      setTimeLeft(prev => {
        const newTime = prev - (1 / 60); // Decrease by 1 second (converted to minutes)
        
        if (newTime <= 0) {
          clearInterval(interval);
          if (onExpired) {
            onExpired();
          }
          return 0;
        }
        
        return newTime;
      });
    }, 1000); // Update every second

    return () => clearInterval(interval);
  }, [open, minutesUntilExpiration, onExpired]);

  const handleRefresh = async () => {
    setIsRefreshing(true);
    setError(null);
    
    try {
      const success = await onRefresh();
      
      if (success) {
        onDismiss();
      } else {
        setError('Failed to refresh session. Please try again.');
      }
    } catch (err) {
      setError('An error occurred while refreshing your session.');
    } finally {
      setIsRefreshing(false);
    }
  };

  const formatTimeLeft = (minutes: number): string => {
    const mins = Math.floor(minutes);
    const secs = Math.floor((minutes - mins) * 60);
    
    if (mins > 0) {
      return `${mins} minute${mins !== 1 ? 's' : ''} ${secs} second${secs !== 1 ? 's' : ''}`;
    }
    return `${secs} second${secs !== 1 ? 's' : ''}`;
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onDismiss()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-yellow-100 dark:bg-yellow-900/20">
              <Warning size={24} weight="fill" className="text-yellow-600 dark:text-yellow-500" />
            </div>
            <DialogTitle>Session Timeout Warning</DialogTitle>
          </div>
          <DialogDescription>
            Your session is about to expire due to inactivity.
          </DialogDescription>
        </DialogHeader>

        <div className="py-4">
          <Alert>
            <AlertDescription>
              <div className="space-y-2">
                <p className="font-medium">
                  Time remaining: <span className="text-yellow-600 dark:text-yellow-500">{formatTimeLeft(timeLeft)}</span>
                </p>
                {canRefresh ? (
                  <p className="text-sm text-muted-foreground">
                    Click "Stay Signed In" to continue your session, or you will be automatically logged out.
                  </p>
                ) : (
                  <p className="text-sm text-muted-foreground">
                    You will be automatically logged out when the timer reaches zero. Please save your work.
                  </p>
                )}
              </div>
            </AlertDescription>
          </Alert>

          {error && (
            <Alert variant="destructive" className="mt-4">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter className="flex-col gap-2 sm:flex-row sm:justify-end">
          {canRefresh && (
            <Button
              onClick={handleRefresh}
              disabled={isRefreshing || timeLeft <= 0}
              className="w-full sm:w-auto"
            >
              {isRefreshing ? (
                <>
                  <ArrowClockwise size={16} className="mr-2 animate-spin" />
                  Refreshing...
                </>
              ) : (
                <>
                  <ArrowClockwise size={16} className="mr-2" />
                  Stay Signed In
                </>
              )}
            </Button>
          )}
          <Button
            variant="outline"
            onClick={onDismiss}
            disabled={isRefreshing}
            className="w-full sm:w-auto"
          >
            Dismiss
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
