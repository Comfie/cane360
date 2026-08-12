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

    const problem = /** @type {{ errors?: Record<string, string[] | string>, detail?: string, title?: string, message?: string }} */ (
      source.result ?? parsedResponse ?? error
    );
    const validationMessages = Object.values(problem.errors ?? {})
      .flatMap((messages) => Array.isArray(messages) ? messages : [messages])
      .filter(Boolean);

    if (validationMessages.length > 0) return validationMessages.join(' ');
    if (problem.detail) return problem.detail;
    if (problem.title) return problem.title;
    if (problem.message) return problem.message;
  }

  return 'Cane360 could not complete the request. Please try again.';
}
