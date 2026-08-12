import { createContext, useContext, useEffect, useState } from 'react';
import { LoginRequest, RegisterRequest, UsersClient } from '../../web-api-client';

const client = new UsersClient();

/**
 * @typedef {object} AuthContextValue
 * @property {boolean} isAuthenticated
 * @property {boolean} isLoading
 * @property {string | null} accountEmail
 * @property {(email: string, password: string) => Promise<void>} login
 * @property {(email: string, password: string) => Promise<void>} register
 * @property {() => Promise<void>} logout
 */

const AuthContext = createContext(/** @type {AuthContextValue | null} */ (null));

/** @param {{ children: import('react').ReactNode }} props */
export function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [accountEmail, setAccountEmail] = useState(/** @type {string | null} */ (null));

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

  /** @param {string} email @param {string} password */
  const login = async (email, password) => {
    await client.login(true, undefined, new LoginRequest({ email, password }));
    setIsAuthenticated(true);
    setAccountEmail(email);
  };

  /** @param {string} email @param {string} password */
  const register = (email, password) => client.register(new RegisterRequest({ email, password }));

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

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return context;
}
