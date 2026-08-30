import { useEffect, useState } from 'react';
import type { Dispatch, SetStateAction } from 'react';
import { FarmSetupClient, FarmSetupDto } from '../../web-api-client';
import { getApiError } from '../apiError';

export { getApiError } from '../apiError';

export const farmSetupClient = new FarmSetupClient();

export interface FarmSetupState {
  setup: FarmSetupDto | null;
  setSetup: Dispatch<SetStateAction<FarmSetupDto | null>>;
  error: string;
  setError: Dispatch<SetStateAction<string>>;
  isLoading: boolean;
}

export function useFarmSetup(): FarmSetupState {
  const [setup, setSetup] = useState<FarmSetupDto | null>(null);
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
