import { LoginRequest, RegisterRequest, UsersClient } from '../../web-api-client';
import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';

const client = new UsersClient();

interface AuthContextValue {
  isAuthenticated: boolean;
  isLoading: boolean;
  accountEmail: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [accountEmail, setAccountEmail] = useState<string | null>(null);

  useEffect(() => {
    client.info()
      .then((account) => {
        setIsAuthenticated(true);
        setAccountEmail(account.email ?? null);
      })
      .catch(() => {
        setIsAuthenticated(false);
        setAccountEmail(null);
      })
      .finally(() => setIsLoading(false));
  }, []);

  const login = async (email: string, password: string): Promise<void> => {
    await client.login(true, undefined, new LoginRequest({ email, password }));
    setIsAuthenticated(true);
    setAccountEmail(email);
  };

  const register = (email: string, password: string): Promise<void> => client.register(new RegisterRequest({ email, password }));

  const logout = async () => {
    await client.logout();
    setIsAuthenticated(false);
    setAccountEmail(null);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, accountEmail, login, register, logout }}>
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
