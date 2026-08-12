import { useEffect, useState } from 'react';
import { FarmSetupClient } from '../../web-api-client';
import { getApiError } from '../apiError';

export { getApiError } from '../apiError';

export const farmSetupClient = new FarmSetupClient();

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
