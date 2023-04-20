export const dateTimeFormat = new Intl.DateTimeFormat(import.meta.env.VITE_LOCALE, {
  dateStyle: "medium",
  timeStyle: "short",
});
export const currencyFormat = new Intl.NumberFormat(import.meta.env.VITE_LOCALE, {
  style: "currency",
  currency: "AUD",
});

export const API_BASE_URI = `http://${import.meta.env.VITE_API_BASE_URI_HOST}`;
