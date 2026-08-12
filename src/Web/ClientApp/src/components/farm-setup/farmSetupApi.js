import { useEffect, useState } from 'react';
import { FarmSetupClient } from '../../web-api-client';

export const farmSetupClient = new FarmSetupClient();

/** @param {unknown} error */
export function getApiError(error) {
  if (error && typeof error === 'object') {
    const source = /** @type {{ result?: unknown, response?: string }} */ (error);
    let parsedResponse;
    try {
      parsedResponse = source.response ? JSON.parse(source.response) : undefined;
    } catch {
      parsedResponse = undefined;
    }
    const problem = /** @type {{ errors?: Record<string, string[]>, detail?: string, title?: string, message?: string }} */ (source.result ?? parsedResponse ?? error);
    const validationMessages = Object.values(problem.errors ?? {}).flat().filter(Boolean);

    if (validationMessages.length > 0) return validationMessages.join(' ');
    if (problem.detail) return problem.detail;
    if (problem.title) return problem.title;
    if (problem.message) return problem.message;
  }

  return 'Cane360 could not complete the request. Please try again.';
}

export function useFarmSetup() {
  const [setup, setSetup] = useState(/** @type {import('../../web-api-client').FarmSetupDto | null} */ (null));
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isCurrent = true;

    farmSetupClient.farmSetup()
      .then((result) => {
        if (isCurrent) setSetup(result);
      })
      .catch((requestError) => {
        if (isCurrent) setError(getApiError(requestError));
      })
      .finally(() => {
        if (isCurrent) setIsLoading(false);
      });

    return () => {
      isCurrent = false;
    };
  }, []);

  return { setup, setSetup, error, setError, isLoading };
}
