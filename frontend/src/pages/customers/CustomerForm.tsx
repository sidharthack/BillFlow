import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Spinner } from '../../components/ui/Spinner';

const schema = z.object({
  name:       z.string().min(1, 'Name is required'),
  email:      z.string().email('Invalid email'),
  phone:      z.string().optional(),
  gstNumber:  z.string().optional(),
  panNumber:  z.string().optional(),
  address: z.object({
    line1:   z.string().min(1, 'Address line 1 is required'),
    line2:   z.string().optional(),
    city:    z.string().min(1, 'City is required'),
    state:   z.string().min(1, 'State is required'),
    pinCode: z.string().min(6, 'Valid PIN code required'),
    country: z.string().optional(),
  }).optional(),
});

export type CustomerFormData = z.infer<typeof schema>;

interface Props {
  defaultValues?: Partial<CustomerFormData>;
  onSubmit: (data: CustomerFormData) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

export function CustomerForm({
  defaultValues,
  onSubmit,
  isLoading,
  submitLabel = 'Save',
}: Props) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CustomerFormData>({
    resolver: zodResolver(schema),
    defaultValues,
  });

  useEffect(() => {
    if (defaultValues) reset(defaultValues);
  }, [defaultValues, reset]);

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">

      {/* Basic info */}
      <div>
        <h3 className="text-xs font-semibold text-gray-400 uppercase
                       tracking-wider mb-3">
          Basic Information
        </h3>
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2">
            <label className="label">Full Name *</label>
            <input
              className="input"
              placeholder="Acme Corporation"
              {...register('name')}
            />
            {errors.name && (
              <p className="text-xs text-red-500 mt-1">
                {errors.name.message}
              </p>
            )}
          </div>

          <div>
            <label className="label">Email *</label>
            <input
              className="input"
              type="email"
              placeholder="billing@acme.com"
              {...register('email')}
            />
            {errors.email && (
              <p className="text-xs text-red-500 mt-1">
                {errors.email.message}
              </p>
            )}
          </div>

          <div>
            <label className="label">Phone</label>
            <input
              className="input"
              placeholder="+91-9876543210"
              {...register('phone')}
            />
          </div>
        </div>
      </div>

      {/* Tax info */}
      <div>
        <h3 className="text-xs font-semibold text-gray-400 uppercase
                       tracking-wider mb-3">
          Tax Information
        </h3>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="label">GST Number</label>
            <input
              className="input"
              placeholder="27AABCT1332L1ZB"
              {...register('gstNumber')}
            />
          </div>
          <div>
            <label className="label">PAN Number</label>
            <input
              className="input"
              placeholder="AABCT1332L"
              {...register('panNumber')}
            />
          </div>
        </div>
      </div>

      {/* Address */}
      <div>
        <h3 className="text-xs font-semibold text-gray-400 uppercase
                       tracking-wider mb-3">
          Address
        </h3>
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2">
            <label className="label">Line 1 *</label>
            <input
              className="input"
              placeholder="12 Tech Park, Whitefield"
              {...register('address.line1')}
            />
            {errors.address?.line1 && (
              <p className="text-xs text-red-500 mt-1">
                {errors.address.line1.message}
              </p>
            )}
          </div>

          <div className="col-span-2">
            <label className="label">Line 2</label>
            <input
              className="input"
              placeholder="Near ITPL Main Road"
              {...register('address.line2')}
            />
          </div>

          <div>
            <label className="label">City *</label>
            <input
              className="input"
              placeholder="Bangalore"
              {...register('address.city')}
            />
            {errors.address?.city && (
              <p className="text-xs text-red-500 mt-1">
                {errors.address.city.message}
              </p>
            )}
          </div>

          <div>
            <label className="label">State *</label>
            <input
              className="input"
              placeholder="Karnataka"
              {...register('address.state')}
            />
            {errors.address?.state && (
              <p className="text-xs text-red-500 mt-1">
                {errors.address.state.message}
              </p>
            )}
          </div>

          <div>
            <label className="label">PIN Code *</label>
            <input
              className="input"
              placeholder="560066"
              {...register('address.pinCode')}
            />
            {errors.address?.pinCode && (
              <p className="text-xs text-red-500 mt-1">
                {errors.address.pinCode.message}
              </p>
            )}
          </div>

          <div>
            <label className="label">Country</label>
            <input
              className="input"
              placeholder="India"
              defaultValue="India"
              {...register('address.country')}
            />
          </div>
        </div>
      </div>

      {/* Submit */}
      <div className="flex justify-end pt-2 border-t border-gray-100">
        <button
          type="submit"
          disabled={isLoading}
          className="btn-primary px-6"
        >
          {isLoading ? (
            <>
              <Spinner size="sm" />
              Saving...
            </>
          ) : submitLabel}
        </button>
      </div>
    </form>
  );
}