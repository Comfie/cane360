interface ApiErrorSource {
  status?: unknown;
  result?: unknown;
  response?: unknown;
}

interface ProblemDetails {
  status?: unknown;
  traceId?: unknown;
  errors?: Record<string, unknown>;
  detail?: unknown;
  title?: unknown;
  message?: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object';
}

function asApiErrorSource(value: unknown): ApiErrorSource | undefined {
  return isRecord(value) ? value : undefined;
}

function asProblemDetails(value: unknown): ProblemDetails | undefined {
  return isRecord(value) ? value : undefined;
}

function asMessage(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined;
}

export function getApiError(error: unknown): string {
  const source = asApiErrorSource(error);

  if (source) {
    let parsedResponse: unknown;
    try {
      parsedResponse = typeof source.response === 'string' ? JSON.parse(source.response) : undefined;
    } catch {
      parsedResponse = undefined;
    }

    const problem = asProblemDetails(source.result ?? parsedResponse ?? error);
    const status = typeof source.status === 'number' ? source.status : problem?.status;
    const reference = asMessage(problem?.traceId);
    const support = reference && /^[A-Za-z0-9:._-]{1,128}$/.test(reference) ? ` Support reference: ${reference}.` : '';
    if (typeof status === 'number' && status >= 500) {
      return `Cane360 could not complete the request. Refresh to check whether the action completed before trying again.${support}`;
    }
    if (status === 401) return 'Your session has expired. Log in again to continue.';
    if (status === 403) return 'You do not have permission to complete this action. Ask your Grower for help.';
    if (status === 404) return 'This record is unavailable in your current workspace. Refresh the list and try again.';
    const validationMessages = problem
      ? Object.values(problem.errors ?? {})
        .flatMap((messages) => Array.isArray(messages) ? messages : [messages])
        .map(asMessage)
        .filter((message): message is string => message !== undefined)
      : [];

    if (validationMessages.length > 0) return validationMessages.join(' ');
    const detail = asMessage(problem?.detail);
    if (detail) return detail;
    const title = asMessage(problem?.title);
    if (title) return title;
    const message = asMessage(problem?.message);
    if (message) return message;
  }

  return 'Cane360 could not complete the request. Please try again.';
}
