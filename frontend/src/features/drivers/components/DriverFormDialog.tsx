import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { FormTextarea } from '@/components/common/FormTextarea'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { Driver } from '@/types/models'
import { useCreateDriver, useUpdateDriver } from '../hooks'

const phoneRegex = /^\+?[0-9\s\-()]{7,20}$/
const optionalDate = z.string().optional()

const schema = z.object({
  firstName: z.string().min(1, 'Ad zorunlu'),
  lastName: z.string().min(1, 'Soyad zorunlu'),
  nationalId: z.string().optional(),
  phone: z.string().optional().refine((v) => !v || phoneRegex.test(v), 'Geçerli telefon girin'),
  email: z.string().optional().refine((v) => !v || z.string().email().safeParse(v).success, 'Geçerli e-posta girin'),
  licenseNo: z.string().optional(),
  licenseClass: z.string().optional(),
  licenseStartDate: optionalDate,
  licenseExpiryDate: optionalDate,
  srcStartDate: optionalDate,
  srcEndDate: optionalDate,
  psychotechnicalStartDate: optionalDate,
  psychotechnicalEndDate: optionalDate,
  visaStartDate: optionalDate,
  visaEndDate: optionalDate,
  passportStartDate: optionalDate,
  passportEndDate: optionalDate,
  documentNote: z.string().optional(),
}).superRefine((values, ctx) => {
  const ranges: Array<[keyof typeof values, keyof typeof values, string]> = [
    ['licenseStartDate', 'licenseExpiryDate', 'Ehliyet bitiş tarihi başlangıçtan önce olamaz'],
    ['srcStartDate', 'srcEndDate', 'SRC bitiş tarihi başlangıçtan önce olamaz'],
    ['psychotechnicalStartDate', 'psychotechnicalEndDate', 'Psikoteknik bitiş tarihi başlangıçtan önce olamaz'],
    ['visaStartDate', 'visaEndDate', 'Vize bitiş tarihi başlangıçtan önce olamaz'],
    ['passportStartDate', 'passportEndDate', 'Pasaport bitiş tarihi başlangıçtan önce olamaz'],
  ]
  for (const [startKey, endKey, message] of ranges) {
    const start = values[startKey]
    const end = values[endKey]
    if (start && end && end < start) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: [endKey], message })
    }
  }
})
type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  driver?: Driver | null
}

const emptyToUndefined = (value?: string) => value || undefined

export function DriverFormDialog({ open, onOpenChange, driver }: Props) {
  const isEdit = !!driver
  const create = useCreateDriver()
  const update = useUpdateDriver()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        firstName: driver?.firstName ?? '',
        lastName: driver?.lastName ?? '',
        nationalId: driver?.nationalId ?? '',
        phone: driver?.phone ?? '',
        email: driver?.email ?? '',
        licenseNo: driver?.licenseNo ?? '',
        licenseClass: driver?.licenseClass ?? '',
        licenseStartDate: driver?.licenseStartDate ?? '',
        licenseExpiryDate: driver?.licenseExpiryDate ?? '',
        srcStartDate: driver?.srcStartDate ?? '',
        srcEndDate: driver?.srcEndDate ?? '',
        psychotechnicalStartDate: driver?.psychotechnicalStartDate ?? '',
        psychotechnicalEndDate: driver?.psychotechnicalEndDate ?? '',
        visaStartDate: driver?.visaStartDate ?? '',
        visaEndDate: driver?.visaEndDate ?? '',
        passportStartDate: driver?.passportStartDate ?? '',
        passportEndDate: driver?.passportEndDate ?? '',
        documentNote: driver?.documentNote ?? '',
      })
    }
  }, [open, driver, reset])

  async function onSubmit(values: FormValues) {
    const documentBody = {
      licenseNo: emptyToUndefined(values.licenseNo),
      licenseClass: emptyToUndefined(values.licenseClass),
      licenseStartDate: emptyToUndefined(values.licenseStartDate),
      licenseExpiryDate: emptyToUndefined(values.licenseExpiryDate),
      srcStartDate: emptyToUndefined(values.srcStartDate),
      srcEndDate: emptyToUndefined(values.srcEndDate),
      psychotechnicalStartDate: emptyToUndefined(values.psychotechnicalStartDate),
      psychotechnicalEndDate: emptyToUndefined(values.psychotechnicalEndDate),
      visaStartDate: emptyToUndefined(values.visaStartDate),
      visaEndDate: emptyToUndefined(values.visaEndDate),
      passportStartDate: emptyToUndefined(values.passportStartDate),
      passportEndDate: emptyToUndefined(values.passportEndDate),
      documentNote: emptyToUndefined(values.documentNote),
    }

    try {
      if (isEdit && driver) {
        await update.mutateAsync({
          id: driver.id,
          body: { phone: emptyToUndefined(values.phone), email: emptyToUndefined(values.email), ...documentBody },
        })
      } else {
        await create.mutateAsync({
          firstName: values.firstName,
          lastName: values.lastName,
          nationalId: emptyToUndefined(values.nationalId),
          phone: emptyToUndefined(values.phone),
          email: emptyToUndefined(values.email),
          ...documentBody,
        })
      }
      onOpenChange(false)
    } catch {
      /* toast handled */
    }
  }

  const pending = create.isPending || update.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Şoförü Düzenle' : 'Yeni Şoför'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormInput label="Ad" disabled={isEdit} error={errors.firstName?.message} {...register('firstName')} />
            <FormInput label="Soyad" disabled={isEdit} error={errors.lastName?.message} {...register('lastName')} />
          </div>
          <FormInput label="TC Kimlik No" disabled={isEdit} error={errors.nationalId?.message} {...register('nationalId')} />
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormInput label="Telefon" error={errors.phone?.message} {...register('phone')} />
            <FormInput label="E-posta" type="email" error={errors.email?.message} {...register('email')} />
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormInput label="Ehliyet No" error={errors.licenseNo?.message} {...register('licenseNo')} />
            <FormInput label="Ehliyet Sınıfı" error={errors.licenseClass?.message} {...register('licenseClass')} />
            <FormInput label="Ehliyet Başlangıç Tarihi" type="date" error={errors.licenseStartDate?.message} {...register('licenseStartDate')} />
            <FormInput label="Ehliyet Bitiş Tarihi" type="date" error={errors.licenseExpiryDate?.message} {...register('licenseExpiryDate')} />
            <FormInput label="SRC Başlangıç Tarihi" type="date" error={errors.srcStartDate?.message} {...register('srcStartDate')} />
            <FormInput label="SRC Bitiş Tarihi" type="date" error={errors.srcEndDate?.message} {...register('srcEndDate')} />
            <FormInput label="Psikoteknik Başlangıç Tarihi" type="date" error={errors.psychotechnicalStartDate?.message} {...register('psychotechnicalStartDate')} />
            <FormInput label="Psikoteknik Bitiş Tarihi" type="date" error={errors.psychotechnicalEndDate?.message} {...register('psychotechnicalEndDate')} />
            <FormInput label="Vize Başlangıç Tarihi" type="date" error={errors.visaStartDate?.message} {...register('visaStartDate')} />
            <FormInput label="Vize Bitiş Tarihi" type="date" error={errors.visaEndDate?.message} {...register('visaEndDate')} />
            <FormInput label="Pasaport Başlangıç Tarihi" type="date" error={errors.passportStartDate?.message} {...register('passportStartDate')} />
            <FormInput label="Pasaport Bitiş Tarihi" type="date" error={errors.passportEndDate?.message} {...register('passportEndDate')} />
          </div>
          <FormTextarea label="Belge Notu" error={errors.documentNote?.message} {...register('documentNote')} />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>Vazgeç</Button>
            <Button type="submit" disabled={pending}>{pending ? 'Kaydediliyor...' : 'Kaydet'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
