import { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import type { Resolver } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, Trash2, Calculator } from 'lucide-react';
import { useCustomers } from '../../hooks/useCustomers';
import { Spinner } from '../../components/ui/Spinner';
import { formatCurrency } from '../../utils/format';

const lineItemSchema = z.object({
  description: z.string().min(1, 'Description required'),
  quantity:    z.coerce.number().min(1,    'Min 1'),
  unitPrice:   z.coerce.number().min(0.01, 'Required'),
});

const schema = z.object({
  customerId: z.coerce.number().min(1, 'Select a customer'),
  lineItems:  z.array(lineItemSchema).min(1, 'Add at least one item'),
  notes:      z.string().optional(),
  dueDate:    z.string().optional(),
});

export type CreateInvoiceFormData = z.infer<typeof schema>;

interface Props {
  onSubmit: (data: CreateInvoiceFormData) => void;
  isLoading?: boolean;
}

export function CreateInvoiceForm({ onSubmit, isLoading }: Props) {
  const { data: customers = [], isLoading: loadingCustomers } = useCustomers();
  const [taxRate] = useState(0.18); // pulled from tenant settings in future

  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<CreateInvoiceFormData>({
    resolver: zodResolver(schema) as Resolver<CreateInvoiceFormData>,
    defaultValues: {
      lineItems: [{ description: '', quantity: 1, unitPrice: 0 }],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'lineItems',
  });

  // Watch line items for live total calculation
  const lineItems  = watch('lineItems');
  const subTotal   = lineItems.reduce(
    (sum, item) =>
      sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0),
    0
  );
  const taxAmount  = subTotal * taxRate;
  const total      = subTotal + taxAmount;

  // Min due date = today
  const today = new Date().toISOString().split('T')[0];

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">

      {/* Customer + Due date row */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="label">Customer *</label>
          {loadingCustomers ? (
            <div className="input flex items-center gap-2 text-gray-400">
              <Spinner size="sm" /> Loading...
            </div>
          ) : (
            <select className="input" {...register('customerId')}>
              <option value="">Select customer...</option>
              {customers
                .filter(c => c.status === 'Active')
                .map(c => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
            </select>
          )}
          {errors.customerId && (
            <p className="text-xs text-red-500 mt-1">
              {errors.customerId.message}
            </p>
          )}
        </div>

        <div>
          <label className="label">Due Date</label>
          <input
            type="date"
            className="input"
            min={today}
            {...register('dueDate')}
          />
        </div>
      </div>

      {/* Line items */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <label className="label mb-0">Line Items *</label>
          <button
            type="button"
            onClick={() =>
              append({ description: '', quantity: 1, unitPrice: 0 })
            }
            className="btn-secondary text-xs px-3 py-1.5"
          >
            <Plus className="h-3.5 w-3.5" />
            Add Item
          </button>
        </div>

        {errors.lineItems?.root && (
          <p className="text-xs text-red-500 mb-2">
            {errors.lineItems.root.message}
          </p>
        )}

        {/* Line item header */}
        <div className="grid grid-cols-12 gap-2 mb-1 px-1">
          <p className="col-span-6 text-xs text-gray-400 font-medium">
            Description
          </p>
          <p className="col-span-2 text-xs text-gray-400 font-medium text-right">
            Qty
          </p>
          <p className="col-span-3 text-xs text-gray-400 font-medium text-right">
            Unit Price
          </p>
          <p className="col-span-1" />
        </div>

        <div className="space-y-2">
          {fields.map((field, index) => {
            const qty   = Number(lineItems[index]?.quantity) || 0;
            const price = Number(lineItems[index]?.unitPrice) || 0;
            const amt   = qty * price;

            return (
              <div key={field.id} className="grid grid-cols-12 gap-2 items-start">
                {/* Description */}
                <div className="col-span-6">
                  <input
                    className="input text-sm"
                    placeholder="Description of service..."
                    {...register(`lineItems.${index}.description`)}
                  />
                  {errors.lineItems?.[index]?.description && (
                    <p className="text-xs text-red-500 mt-0.5">
                      {errors.lineItems[index]?.description?.message}
                    </p>
                  )}
                </div>

                {/* Quantity */}
                <div className="col-span-2">
                  <input
                    type="number"
                    min={1}
                    className="input text-sm text-right"
                    {...register(`lineItems.${index}.quantity`)}
                  />
                </div>

                {/* Unit price */}
                <div className="col-span-3">
                  <input
                    type="number"
                    min={0}
                    step={0.01}
                    className="input text-sm text-right"
                    placeholder="0.00"
                    {...register(`lineItems.${index}.unitPrice`)}
                  />
                </div>

                {/* Remove */}
                <div className="col-span-1 flex justify-center pt-2">
                  <button
                    type="button"
                    onClick={() => remove(index)}
                    disabled={fields.length === 1}
                    className="text-gray-300 hover:text-red-500 transition-colors
                               disabled:opacity-30 disabled:cursor-not-allowed"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>

                {/* Line amount */}
                {amt > 0 && (
                  <div className="col-span-12 -mt-1 pr-8 text-right">
                    <span className="text-xs text-gray-400">
                      = {formatCurrency(amt)}
                    </span>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Totals */}
      {subTotal > 0 && (
        <div className="rounded-lg bg-gray-50 p-4 space-y-2">
          <div className="flex justify-between text-sm text-gray-600">
            <span>Subtotal</span>
            <span className="font-medium">{formatCurrency(subTotal)}</span>
          </div>
          <div className="flex justify-between text-sm text-gray-600">
            <span>GST ({(taxRate * 100).toFixed(0)}%)</span>
            <span className="font-medium">{formatCurrency(taxAmount)}</span>
          </div>
          <div className="flex justify-between text-base font-bold
                          text-gray-900 pt-2 border-t border-gray-200">
            <span>Total</span>
            <span className="text-primary-600">{formatCurrency(total)}</span>
          </div>
        </div>
      )}

      {/* Notes */}
      <div>
        <label className="label">Notes</label>
        <textarea
          className="input resize-none h-20"
          placeholder="Payment terms, thank you note, bank details..."
          {...register('notes')}
        />
      </div>

      {/* Submit */}
      <div className="flex justify-end pt-2 border-t border-gray-100">
        <button
          type="submit"
          disabled={isLoading || subTotal === 0}
          className="btn-primary px-6"
        >
          {isLoading ? (
            <>
              <Spinner size="sm" />
              Creating...
            </>
          ) : (
            <>
              <Calculator className="h-4 w-4" />
              Create Invoice
            </>
          )}
        </button>
      </div>
    </form>
  );
}