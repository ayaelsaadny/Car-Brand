import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * North American Numbering Plan (NANP) Phone Regex:
 * Format: +1 (Country Code for Canada & USA), 3-digit Area Code, and 7-digit local number.
 * Supports:
 * - +1 647 952 5412 (the user's example)
 * - +1 647  952 5412 (multiple spaces)
 * - +1 (647) 952-5412
 * - +1-647-952-5412
 * - +16479525412
 * - 1 647 952 5412 / 16479525412
 * - Standard 10-digit North American numbers: 647 952 5412, (647) 952-5412, 647-952-5412
 */
export const NA_PHONE_REGEX = /^(\+?1\s*[-.]?\s*)?\(?[2-9][0-9]{2}\)?\s*[-.]?\s*[0-9]{3}\s*[-.]?\s*[0-9]{4}$/;
export const NA_PHONE_PATTERN_STR = '^(\\+?1\\s*[-.]?\\s*)?\\(?[2-9][0-9]{2}\\)?\\s*[-.]?\\s*[0-9]{3}\\s*[-.]?\\s*[0-9]{4}$';

/**
 * Validates whether the given string is a valid North American phone number.
 */
export function isValidNorthAmericanPhone(phone: string | null | undefined): boolean {
  if (!phone || !phone.trim()) return false;
  return NA_PHONE_REGEX.test(phone.trim());
}

/**
 * Angular ValidatorFn for reactive forms.
 */
export function northAmericanPhoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const val = control.value;
    if (!val || !val.toString().trim()) {
      return null; // Empty values handled by Validators.required if needed
    }
    return isValidNorthAmericanPhone(val.toString()) ? null : { invalidPhone: true };
  };
}

/**
 * Auto-formats or normalizes a phone string into standard display format: +1 XXX XXX XXXX
 */
export function formatNorthAmericanPhone(value: string | null | undefined): string {
  if (!value) return '';
  const trimmed = value.trim();
  const digits = trimmed.replace(/\D/g, '');

  if (digits.length === 11 && digits.startsWith('1')) {
    const area = digits.slice(1, 4);
    const prefix = digits.slice(4, 7);
    const line = digits.slice(7, 11);
    return `+1 ${area} ${prefix} ${line}`;
  }

  if (digits.length === 10) {
    const area = digits.slice(0, 3);
    const prefix = digits.slice(3, 6);
    const line = digits.slice(6, 10);
    return `+1 ${area} ${prefix} ${line}`;
  }

  return trimmed;
}
