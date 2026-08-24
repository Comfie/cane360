import {
  CreateInputRequestLineRequest,
  CreateInputRequestRequest,
  CreateInventoryApplicationRuleRequest,
  CreateManagerInvitationRequest,
  CreateStockIssueLineRequest,
  CreateStockIssueRequest,
  DecideInputRequestRequest,
  InputControlsClient,
  PostStockReceiptRequest,
  RequestStockIssueCorrectionRequest,
  ReverseStockIssueRequest,
} from '../../web-api-client';
import { operationKey } from './inventoryApi';

export const inputControlsClient = new InputControlsClient();

/** @param {string | undefined} activityId */
export const loadInputControls = (activityId) => inputControlsClient.inputControls(activityId);

/** @param {import('../../web-api-client').ICreateInventoryApplicationRuleRequest} values */
export const createApplicationRule = (values) => inputControlsClient.rules(
  new CreateInventoryApplicationRuleRequest(values));

/** @param {string} activityId @param {import('../../web-api-client').ICreateInputRequestLineRequest[]} lines */
export const createInputRequest = (activityId, lines) => inputControlsClient.requests(
  new CreateInputRequestRequest({ activityId, lines: lines.map((line) => new CreateInputRequestLineRequest(line)) }));

/** @param {string} requestId @param {number} expectedVersion */
export const submitInputRequest = (requestId, expectedVersion) => inputControlsClient.submit(
  requestId, new PostStockReceiptRequest({ expectedVersion, idempotencyKey: operationKey('request-submit') }));

/** @param {string} requestId @param {number} expectedVersion @param {string} outcome @param {string | undefined} reason */
export const decideInputRequest = (requestId, expectedVersion, outcome, reason) => inputControlsClient.decision(
  requestId, new DecideInputRequestRequest({ expectedVersion, outcome, reason, idempotencyKey: operationKey('request-decision') }));

/** @param {{inputRequestId: string, issueDate: Date, issuerPersonId: string, recipientPersonId: string, lateEntryReason: string | undefined, lines: {inputRequestLineId: string, inventoryLotId: string | undefined, quantity: number}[]}} values */
export const createStockIssue = (values) => inputControlsClient.issues(new CreateStockIssueRequest({
  ...values,
  lines: values.lines.map((line) => new CreateStockIssueLineRequest(line)),
}));

/** @param {string} issueId @param {number} expectedVersion */
export const postStockIssue = (issueId, expectedVersion) => inputControlsClient.post(
  issueId, new PostStockReceiptRequest({ expectedVersion, idempotencyKey: operationKey('issue-post') }));

/** @param {string} issueId @param {number} expectedVersion @param {string} reason */
export const requestIssueCorrection = (issueId, expectedVersion, reason) => inputControlsClient.correction(
  issueId, new RequestStockIssueCorrectionRequest({ expectedVersion, reason }));

/** @param {string} issueId @param {number} expectedVersion @param {string} reason */
export const reverseStockIssue = (issueId, expectedVersion, reason) => inputControlsClient.reverse(
  issueId, new ReverseStockIssueRequest({ expectedVersion, reason, idempotencyKey: operationKey('issue-reversal') }));

/** @param {string} personId @param {number} expiresInHours */
export const createManagerInvitation = (personId, expiresInHours) => inputControlsClient.managerInvitations(
  new CreateManagerInvitationRequest({ personId, expiresInHours }));
