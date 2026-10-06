import {
  InventoryCategoriesClient, CreateInventoryCategoryRequest, UpdateInventoryCategoryRequest,
  SetInventoryCategoryActiveRequest,
} from '../../web-api-client';
import type {ICreateInventoryCategoryRequest, IUpdateInventoryCategoryRequest} from '../../web-api-client';

const client = new InventoryCategoriesClient();

export function createCategory(values: ICreateInventoryCategoryRequest) {
  return client.createInventoryCategory(new CreateInventoryCategoryRequest(values));
}

export function updateCategory(id: string, values: IUpdateInventoryCategoryRequest) {
  return client.updateInventoryCategory(id, new UpdateInventoryCategoryRequest(values));
}

export function setCategoryActive(id: string, active: boolean, expectedVersion: number) {
  return client.setInventoryCategoryActive(id, new SetInventoryCategoryActiveRequest({active, expectedVersion}));
}
