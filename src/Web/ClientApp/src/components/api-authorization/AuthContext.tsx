import { LoginRequest, RegisterRequest, SessionClient, UsersClient } from '../../web-api-client';
import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';

const client = new UsersClient();
const sessionClient = new SessionClient();

interface SessionState {
  hasTenant: boolean;
  role: string | null;
}

const emptySession: SessionState = { hasTenant: false, role: null };

interface AuthContextValue {
  isAuthenticated: boolean;
  isLoading: boolean;
  accountEmail: string | null;
  session: SessionState;
  login: (email: string, password: string) => Promise<SessionState>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<SessionState>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [accountEmail, setAccountEmail] = useState<string | null>(null);
  const [session, setSession] = useState<SessionState>(emptySession);

  const refreshSession = async (): Promise<SessionState> => {
    try {
      const summary = await sessionClient.getCurrentSession();
      const nextSession: SessionState = { hasTenant: summary.hasTenant, role: summary.role ?? null };
      setSession(nextSession);
      return nextSession;
    } catch {
      setSession(emptySession);
      return emptySession;
    }
  };

  useEffect(() => {
    client.info()
      .then(async (account) => {
        setIsAuthenticated(true);
        setAccountEmail(account.email ?? null);
        await refreshSession();
      })
      .catch(() => {
        setIsAuthenticated(false);
        setAccountEmail(null);
      })
      .finally(() => setIsLoading(false));
  }, []);

  const login = async (email: string, password: string): Promise<SessionState> => {
    await client.login(true, undefined, new LoginRequest({ email, password }));
    setIsAuthenticated(true);
    setAccountEmail(email);
    return refreshSession();
  };

  const register = (email: string, password: string): Promise<void> => client.register(new RegisterRequest({ email, password }));

  const logout = async () => {
    await client.logout();
    setIsAuthenticated(false);
    setAccountEmail(null);
    setSession(emptySession);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, accountEmail, session, login, register, logout, refreshSession }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return context;
}
