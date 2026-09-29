import { attributeInput } from './attribute-input';
describe('attributeInput', () => {
  it('preserves text whitespace and safely encodes quotes and control characters', () => {
    const text = '  "text"\nnext  ';
    expect(JSON.parse(attributeInput('Text', text).body!)).toEqual({ dataType: 0, value: text });
    expect(attributeInput('Text', ' ').body).toBeNull();
  });

  it('preserves the complete Int64 range and rejects overflow and fractions', () => {
    expect(attributeInput('Integer', '9223372036854775807').body).toBe(
      '{"dataType":1,"value":9223372036854775807}',
    );
    expect(attributeInput('Integer', '-9223372036854775808').body).toBe(
      '{"dataType":1,"value":-9223372036854775808}',
    );
    expect(attributeInput('Integer', '00012').body).toBe('{"dataType":1,"value":12}');
    for (const value of ['9223372036854775808', '-9223372036854775809', '1.2', '1e3', ''])
      expect(attributeInput('Integer', value).body).toBeNull();
  });

  it('preserves decimal precision and accepts comma input without binary floating point', () => {
    expect(attributeInput('Decimal', '0,1234567890123456789012345678').body).toBe(
      '{"dataType":2,"value":0.1234567890123456789012345678}',
    );
    expect(attributeInput('Decimal', '-0012.500').body).toBe('{"dataType":2,"value":-12.5}');
    expect(attributeInput('Decimal', '79228162514264337593543950335').body).toBe(
      '{"dataType":2,"value":79228162514264337593543950335}',
    );
    for (const value of [
      '79228162514264337593543950336',
      '0.00000000000000000000000000001',
      'NaN',
      '1e3',
      '1,2.3',
      '',
    ])
      expect(attributeInput('Decimal', value).body).toBeNull();
  });

  it('distinguishes false from an absent boolean value', () => {
    expect(JSON.parse(attributeInput('Boolean', 'false').body!)).toEqual({
      dataType: 3,
      value: false,
    });
    expect(JSON.parse(attributeInput('Boolean', 'true').body!)).toEqual({
      dataType: 3,
      value: true,
    });
    expect(attributeInput('Boolean', '').body).toBeNull();
  });
  it('validates calendar dates, including leap years and the DateOnly boundaries', () => {
    for (const value of ['2024-02-29', '0001-01-01', '9999-12-31'])
      expect(JSON.parse(attributeInput('Date', value).body!).value).toBe(value);
    for (const value of ['2023-02-29', '2026-04-31', '0000-01-01', '2026-13-01', ''])
      expect(attributeInput('Date', value).body).toBeNull();
  });

  it('preserves choice order, whitespace and embedded newlines; rejects blanks and duplicates', () => {
    expect(JSON.parse(attributeInput('Choice', ' Linnen ').body!).value).toBe(' Linnen ');
    const choices = ['Second', 'First', 'line\nline'];
    expect(JSON.parse(attributeInput('MultiChoice', JSON.stringify(choices)).body!).value).toEqual(
      choices,
    );
    for (const value of ['[]', '[""]', '["a","a"]', '[1]', 'null', 'bad'])
      expect(attributeInput('MultiChoice', value).body).toBeNull();
  });
  it('refuses unknown types rather than guessing their format', () => {
    expect(attributeInput('FutureType', 'value').body).toBeNull();
  });
});
