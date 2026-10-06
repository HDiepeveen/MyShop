import { ParamMap } from '@angular/router';
import { readListQuery } from '../catalog/list-query';
import { CustomerOrderStatus } from './customer-order.api';
export const customerOrderStatuses: readonly CustomerOrderStatus[] = [
  'awaitingPayment',
  'paid',
  'shipped',
  'cancelled',
  'refunded',
];
export function readCustomerOrderQuery(params: ParamMap) {
  const status = params.get('status');
  return {
    ...readListQuery(params),
    status: customerOrderStatuses.includes(status as CustomerOrderStatus)
      ? (status as CustomerOrderStatus)
      : null,
  };
}
