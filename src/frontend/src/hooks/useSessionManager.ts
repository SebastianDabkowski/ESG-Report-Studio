import { useEffect, useRef, useState, useCallback } from 'react';

/**
 * Session status from the backend
 */
interface SessionStatus {
  isActive: boolean;
  sessionId?: string;
  expiresAt?: string;
  minutesUntilExpiration?: number;
  shouldWarn: boolean;
  canRefresh: boolean;
}

/**
 * Options for the session manager hook
 */
interface UseSessionManagerOptions {
  /**
   * How often to check session status (in milliseconds)
   * Default: 60000 (1 minute)
   */
  checkInterval?: number;
  
  /**
   * Callback when session expires
   */
  onSessionExpired?: () => void;
  
  /**
   * Callback when session warning is triggered
   */
  onSessionWarning?: () => void;
  
  /**
   * API base URL
   */
  apiBaseUrl?: string;
  
  /**
   * Whether to enable session tracking
   * Default: true
   */
  enabled?: boolean;
}

/**
 * Hook for managing session timeout and activity tracking
 */
export function useSessionManager(options: UseSessionManagerOptions = {}) {
  const {
    checkInterval = 60000, // 1 minute
    onSessionExpired,
    onSessionWarning,
    apiBaseUrl = '/api',
    enabled = true
  } = options;

  const [sessionStatus, setSessionStatus] = useState<SessionStatus>({
    isActive: true,
    shouldWarn: false,
    canRefresh: false
  });
  
  const [showWarning, setShowWarning] = useState(false);
  const checkIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const lastActivityRef = useRef<number>(Date.now());

  /**
   * Check session status with the backend
   */
  const checkSessionStatus = useCallback(async () => {
    if (!enabled) return;

    try {
      const response = await fetch(`${apiBaseUrl}/session/status`, {
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        const status: SessionStatus = await response.json();
        setSessionStatus(status);

        if (!status.isActive) {
          // Session expired
          setShowWarning(false);
          if (onSessionExpired) {
            onSessionExpired();
          }
        } else if (status.shouldWarn && !showWarning) {
          // Show warning
          setShowWarning(true);
          if (onSessionWarning) {
            onSessionWarning();
          }
        } else if (!status.shouldWarn && showWarning) {
          // Hide warning if session was refreshed
          setShowWarning(false);
        }
      } else if (response.status === 401) {
        // Unauthorized - session expired
        setSessionStatus({ isActive: false, shouldWarn: false, canRefresh: false });
        setShowWarning(false);
        if (onSessionExpired) {
          onSessionExpired();
        }
      }
    } catch (error) {
      console.error('Failed to check session status:', error);
    }
  }, [apiBaseUrl, enabled, onSessionExpired, onSessionWarning, showWarning]);

  /**
   * Track user activity
   */
  const trackActivity = useCallback(() => {
    if (!enabled) return;
    lastActivityRef.current = Date.now();
  }, [enabled]);

  /**
   * Refresh the current session
   */
  const refreshSession = useCallback(async () => {
    if (!enabled) return false;

    try {
      const response = await fetch(`${apiBaseUrl}/session/refresh`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        setShowWarning(false);
        await checkSessionStatus();
        return true;
      }
      return false;
    } catch (error) {
      console.error('Failed to refresh session:', error);
      return false;
    }
  }, [apiBaseUrl, checkSessionStatus, enabled]);

  /**
   * Logout (end session)
   */
  const logout = useCallback(async () => {
    if (!enabled) return;

    try {
      await fetch(`${apiBaseUrl}/session/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        }
      });
    } catch (error) {
      console.error('Failed to logout:', error);
    }
  }, [apiBaseUrl, enabled]);

  // Set up activity tracking
  useEffect(() => {
    if (!enabled) return;

    const events = ['mousedown', 'keydown', 'scroll', 'touchstart'];
    
    events.forEach(event => {
      document.addEventListener(event, trackActivity);
    });

    return () => {
      events.forEach(event => {
        document.removeEventListener(event, trackActivity);
      });
    };
  }, [trackActivity, enabled]);

  // Set up periodic session status checks
  useEffect(() => {
    if (!enabled) return;

    // Check immediately on mount
    checkSessionStatus();

    // Set up interval for periodic checks
    checkIntervalRef.current = setInterval(checkSessionStatus, checkInterval);

    return () => {
      if (checkIntervalRef.current) {
        clearInterval(checkIntervalRef.current);
      }
    };
  }, [checkInterval, checkSessionStatus, enabled]);

  return {
    sessionStatus,
    showWarning,
    refreshSession,
    logout,
    dismissWarning: () => setShowWarning(false)
  };
}
