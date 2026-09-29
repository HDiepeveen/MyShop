import { attributeTypes } from './attribute-types';
export interface AttributeInput {
  body: string | null;
  error: string;
}
export function attributeInput(type: string, text: string): AttributeInput {
  const code = attributeTypes.find((item) => item.name === type)?.code;
  let value: string;
  switch (type) {
    case 'Decimal': {
      const number = text.trim().replace(',', '.');
      if (!/^-?\d+(\.\d+)?$/.test(number))
        return invalid('Vul een decimaal getal in, met een komma of punt.');
      const negative = number.startsWith('-');
      const [whole, rawFraction = ''] = number.replace(/^-/, '').split('.');
      const fraction = rawFraction.replace(/0+$/, '');
      const integer = whole.replace(/^0+(?=\d)/, '');
      const coefficient = BigInt(integer + fraction);
      if (fraction.length > 28 || coefficient > 79228162514264337593543950335n)
        return invalid(
          'Dit getal kan niet exact worden opgeslagen. Gebruik maximaal 28 decimalen en een waarde binnen het ondersteunde bereik.',
        );
      value =
        (negative && coefficient !== 0n ? '-' : '') + integer + (fraction ? '.' + fraction : '');
      break;
    }
    case 'Integer': {
      const number = text.trim();
      if (!/^-?\d+$/.test(number)) return invalid('Vul een geheel getal in.');
      const integer = BigInt(number);
      if (integer < -9223372036854775808n || integer > 9223372036854775807n)
        return invalid('Dit gehele getal valt buiten het ondersteunde bereik.');
      value = integer.toString();
      break;
    }

    case 'Boolean':
      if (text !== 'true' && text !== 'false') return invalid('Kies ja of nee.');
      value = text;
      break;
    case 'Date': {
      if (!/^\d{4}-\d{2}-\d{2}$/.test(text) || text.startsWith('0000'))
        return invalid('Kies een geldige datum.');
      const date = new Date(text + 'T00:00:00.000Z');
      if (!Number.isFinite(date.getTime()) || date.toISOString().slice(0, 10) !== text)
        return invalid('Kies een geldige datum.');
      value = JSON.stringify(text);
      break;
    }

    case 'Choice':
      if (!text.trim()) return invalid('Vul een keuze in.');
      value = JSON.stringify(text);
      break;
    case 'MultiChoice': {
      let choices: unknown;
      try {
        choices = JSON.parse(text);
      } catch {
        return invalid('Vul geldige keuzes in.');
      }
      if (
        !Array.isArray(choices) ||
        !choices.length ||
        choices.some((item) => typeof item !== 'string' || !item.trim())
      )
        return invalid('Vul minstens één keuze in en laat geen keuzes leeg.');
      if (new Set(choices).size !== choices.length)
        return invalid('Elke keuze mag maar één keer voorkomen.');
      value = JSON.stringify(choices);
      break;
    }
    case 'Text':
      if (!text.trim()) return invalid('Vul een tekst in.');
      value = JSON.stringify(text);
      break;
    default:
      return invalid('Dit soort kenmerk kan hier nog niet worden ingevuld.');
  }
  return { body: '{"dataType":' + code + ',"value":' + value + '}', error: '' };
}
function invalid(error: string): AttributeInput {
  return { body: null, error };
}
