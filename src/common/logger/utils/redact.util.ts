/* eslint-disable @typescript-eslint/no-unsafe-assignment */
/* eslint-disable @typescript-eslint/no-unsafe-return */
/* eslint-disable @typescript-eslint/no-unsafe-member-access */

//este archivo oculta informacion sensible en los logs
//estos datos sensibles seran remplazados por ***REDACTED***

const SENSITIVE_FIELDS: string[] = [
  'password',
  'token',
  'accessToken',
  'refreshToken',
  'authorization',
  'cookie',
  'secret',
  'apiKey',
  'creditCard',
  'nationalId', //Cedula(Latinoamerica), SSN(si es usa), cualquier tipo de documento de identidad.
  'cvv', // Clave de la tarjeta
];

/**
 * Redacta (oculta) información sensible de un objeto
 *
 * @param data - El objeto que puede contener información sensible
 * @returns El mismo objeto pero con campos sensibles reemplazados
 *
 * @example
 * const user = { name: 'Juan', password: '12345', email: 'juan@example.com' };
 * redactSensitiveData(user);
 * // Resultado: { name: 'Juan', password: '***REDACTED***', email: 'juan@example.com' }
 */
export function redactSensitiveData(data: any): any {
  //Si no hay datos retornara igual
  if (!data) {
    return data;
  }

  // si hay un array, aplicar redaccion a cada elemento
  if (Array.isArray(data)) {
    return data.map((item) => redactSensitiveData(item));
  }

  //Si es un objeto primitivo (string, number,boolean) lo retornaremos tal cual.
  if (typeof data !== 'object') {
    return data;
  }

  //creamos una copia del elemento para no modificar el original
  const redacted = { ...data };

  //recorremos cada campo del objeto
  for (const key in redacted) {
    //Verificar si el campo es sensible
    const isSensitive = SENSITIVE_FIELDS.some(
      (field) => key.toLowerCase().includes(field.toLowerCase()),
    );

    if (isSensitive) {
      //Reemplazar el valor con la marca de redaccion
      redacted[key] = '***REDACTED***';
    } else if (typeof redacted[key] === 'object') {
      //si el valor es otro objeto, aplicar redaccion recursivamente
      redacted[key] = redactSensitiveData(redacted[key]);
    }
  }

  return redacted;
}

/**
 * Redacta información sensible de strings (útil para URLs y headers)
 *
 * @param str - El string que puede contener información sensible
 * @returns El string con información sensible reemplazada
 *
 * @example
 * const url = 'https://api.com/users?token=abc123&name=Juan';
 * redactSensitiveString(url);
 * // Resultado: 'https://api.com/users?token=***REDACTED***&name=Juan'
 */
export function redactSensitiveString(str: string): string {
  if (!str || typeof str !== 'string') {
    return str;
  }

  let redacted = str;

  //para cada campo sensible, buscar patrones como "password=valor"
  for (const field of SENSITIVE_FIELDS) {
    //patron regex que busca: field= Cualquier_valor_hasta_el_siguiente_&_o_fin_de_string
    const regex = new RegExp(
      `(${field}=)([^&\\s]+)`,
      'gi', //`g` = global (todas las ocurrencias), `i` = case-insensitive
    );

    //reemplazar el valor con ***REDACTED***
    redacted = redacted.replace(regex, `$1***REDACTED***`);
    //$1 = mantiene "field=", reemplaza solo el valor
  }

  return redacted;
}