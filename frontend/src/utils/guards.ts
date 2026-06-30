export function isEmpty(value: unknown) {
  return value === null || value === undefined || value === '' || (Array.isArray(value) && value.length === 0);
}
