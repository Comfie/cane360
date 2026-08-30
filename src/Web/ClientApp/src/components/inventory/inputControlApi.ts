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
  CreateFieldReceiptRequest,
  CreateFieldReceiptLineRequest,
  CreateInputApplicationRequest,
  CreateInputApplicationLineRequest,
  AttestInputApplicationRequest,
  ConfirmInputApplicationRequest,
  CreateStockReturnRequest,
  CreateStockReturnLineRequest,
  PostStockReturnRequest,
  CreateInventoryLossRequest,
  VersionedInventoryRequest,
  DecideInventoryLossRequest,
} from '../../web-api-client';
import type {
  IAttestInputApplicationRequest,
  IConfirmInputApplicationRequest,
  ICreateFieldReceiptLineRequest,
  ICreateInputApplicationLineRequest,
  ICreateInputRequestLineRequest,
  ICreateInventoryApplicationRuleRequest,
  ICreateInventoryLossRequest,
  ICreateManagerInvitationRequest,
  ICreateStockIssueLineRequest,
  ICreateStockReturnLineRequest,
  IDecideInputRequestRequest,
  IDecideInventoryLossRequest,
} from '../../web-api-client';
import { operationKey } from './inventoryApi';

export const inputControlsClient = new InputControlsClient();

interface StockIssueValues {
  inputRequestId: string;
  issueDate: Date;
  issuerPersonId: string;
  recipientPersonId: string;
  lateEntryReason: string | undefined;
  lines: readonly ICreateStockIssueLineRequest[];
}

interface FieldReceiptValues {
  stockIssueId: string;
  fieldId: string;
  cropCycleId: string;
  activityId: string;
  recipientPersonId: string;
  receivedAt: Date;
  lateEntryReason: string | undefined;
  lines: readonly ICreateFieldReceiptLineRequest[];
}

interface InputApplicationValues {
  activityId: string;
  appliedAt: Date;
  coverageBasis: number;
  verifiedCoverage: number;
  lines: readonly ICreateInputApplicationLineRequest[];
}

interface StockReturnValues {
  activityId: string;
  returnDate: Date;
  senderPersonId: string;
  receiverPersonId: string;
  lines: readonly ICreateStockReturnLineRequest[];
}

export const loadInputControls = (activityId: string | undefined) => inputControlsClient.inputControls(activityId);

export const createApplicationRule = (values: ICreateInventoryApplicationRuleRequest) => inputControlsClient.rules(
  new CreateInventoryApplicationRuleRequest(values));

export const createInputRequest = (activityId: string, lines: readonly ICreateInputRequestLineRequest[]) => inputControlsClient.requests(
  new CreateInputRequestRequest({ activityId, lines: lines.map((line) => new CreateInputRequestLineRequest(line)) }));

export const submitInputRequest = (requestId: string, expectedVersion: number) => inputControlsClient.submit(
  requestId, new PostStockReceiptRequest({ expectedVersion, idempotencyKey: operationKey('request-submit') }));

export const decideInputRequest = (requestId: string, expectedVersion: number, outcome: IDecideInputRequestRequest['outcome'], reason: string | undefined) => inputControlsClient.decision(
  requestId, new DecideInputRequestRequest({ expectedVersion, outcome, reason, idempotencyKey: operationKey('request-decision') }));

export const createStockIssue = (values: StockIssueValues) => inputControlsClient.issues(new CreateStockIssueRequest({
  ...values,
  lines: values.lines.map((line) => new CreateStockIssueLineRequest(line)),
}));

export const postStockIssue = (issueId: string, expectedVersion: number) => inputControlsClient.post(
  issueId, new PostStockReceiptRequest({ expectedVersion, idempotencyKey: operationKey('issue-post') }));

export const requestIssueCorrection = (issueId: string, expectedVersion: number, reason: string) => inputControlsClient.correction(
  issueId, new RequestStockIssueCorrectionRequest({ expectedVersion, reason }));

export const reverseStockIssue = (issueId: string, expectedVersion: number, reason: string) => inputControlsClient.reverse(
  issueId, new ReverseStockIssueRequest({ expectedVersion, reason, idempotencyKey: operationKey('issue-reversal') }));

export const createFieldReceipt = (values: FieldReceiptValues) => inputControlsClient.fieldReceipts(new CreateFieldReceiptRequest({
  ...values, lines: values.lines.map((line) => new CreateFieldReceiptLineRequest(line)),
}));

export const createInputApplication = (values: InputApplicationValues) => inputControlsClient.applications(new CreateInputApplicationRequest({
  ...values, lines: values.lines.map((line) => new CreateInputApplicationLineRequest(line)),
}));

export const attestInputApplication = (id: string, supervisorPersonId: IAttestInputApplicationRequest['supervisorPersonId'], note: IAttestInputApplicationRequest['note'], expectedVersion: number) => inputControlsClient.attestation(
  id, new AttestInputApplicationRequest({ supervisorPersonId, note, expectedVersion }));

export const confirmInputApplication = (id: string, expectedVersion: number, lateConfirmationReason: IConfirmInputApplicationRequest['lateConfirmationReason']) => inputControlsClient.confirmation(
  id, new ConfirmInputApplicationRequest({ expectedVersion, lateConfirmationReason, idempotencyKey: operationKey('application-confirm') }));

export const createStockReturn = (values: StockReturnValues) => inputControlsClient.returns(new CreateStockReturnRequest({
  ...values, lines: values.lines.map((line) => new CreateStockReturnLineRequest(line)),
}));

export const postStockReturn = (id: string, expectedVersion: number) => inputControlsClient.post2(
  id, new PostStockReturnRequest({ expectedVersion, idempotencyKey: operationKey('return-post') }));

export const createInventoryLoss = (values: ICreateInventoryLossRequest) => inputControlsClient.losses(new CreateInventoryLossRequest(values));

export const submitInventoryLoss = (id: string, expectedVersion: number) => inputControlsClient.submit2(
  id, new VersionedInventoryRequest({ expectedVersion }));

export const decideInventoryLoss = (id: string, expectedVersion: number, outcome: IDecideInventoryLossRequest['outcome'], reason: IDecideInventoryLossRequest['reason']) => inputControlsClient.decision2(
  id, new DecideInventoryLossRequest({ expectedVersion, outcome, reason, idempotencyKey: operationKey('loss-decision') }));

export const createManagerInvitation = (personId: ICreateManagerInvitationRequest['personId'], expiresInHours: ICreateManagerInvitationRequest['expiresInHours']) => inputControlsClient.managerInvitations(
  new CreateManagerInvitationRequest({ personId, expiresInHours }));
