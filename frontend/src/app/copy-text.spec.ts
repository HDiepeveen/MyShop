import { TestBed } from '@angular/core/testing';
import { CopyText } from './copy-text';
describe('Copy text control', () => {
  function setup(
    writeText: ReturnType<typeof vi.fn> | null = vi.fn().mockResolvedValue(undefined),
  ) {
    const browser = Object.create(navigator);
    Object.defineProperty(browser, 'clipboard', { value: writeText ? { writeText } : undefined });
    vi.stubGlobal('navigator', browser);
    TestBed.configureTestingModule({ imports: [CopyText] });
    const fixture = TestBed.createComponent(CopyText);
    fixture.componentRef.setInput('text', 'MS-123');
    fixture.componentRef.setInput('label', 'Bestelnummer kopiëren');
    fixture.detectChanges();
    return { fixture, page: fixture.componentInstance, writeText };
  }
  afterEach(() => {
    vi.unstubAllGlobals();
  });
  it('copies the exact text and announces success', async () => {
    const { fixture, page, writeText } = setup();
    await page.copy();
    fixture.detectChanges();
    expect(writeText).toHaveBeenCalledExactlyOnceWith('MS-123');
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain(
      'Gekopieerd',
    );
    expect(page.busy()).toBe(false);
    expect(fixture.nativeElement.classList.contains('print-hide')).toBe(true);
  });
  it('prevents duplicate clicks while a clipboard request is pending', async () => {
    let finish!: () => void;
    const promise = new Promise<void>((resolve) => (finish = resolve));
    const write = vi.fn(() => promise);
    const { fixture, page } = setup(write);
    const pending = page.copy();
    await page.copy();
    fixture.detectChanges();
    expect(write).toHaveBeenCalledOnce();
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBe(
      true,
    );
    finish();
    await pending;
    expect(page.busy()).toBe(false);
  });
  it('offers manual copying when the clipboard is unavailable', async () => {
    const { fixture, page } = setup(null);
    await page.copy();
    fixture.detectChanges();
    expect(page.error()).toContain('kopieer handmatig');
    expect(page.busy()).toBe(false);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });
  it('reports denied clipboard access and permits another attempt', async () => {
    const write = vi.fn().mockRejectedValueOnce(new Error('Denied')).mockResolvedValue(undefined);
    const { page } = setup(write);
    await page.copy();
    expect(page.error()).toContain('niet gelukt');
    expect(page.busy()).toBe(false);
    await page.copy();
    expect(page.error()).toBe('');
    expect(page.message()).toBe('Gekopieerd.');
  });
  it.each(['success', 'failure'])(
    'ignores late %s after the order text changes',
    async (result) => {
      let finish!: () => void, fail!: (error: Error) => void;
      const promise = new Promise<void>((resolve, reject) => {
        finish = resolve;
        fail = reject;
      });
      const { fixture, page } = setup(vi.fn(() => promise));
      const pending = page.copy();
      fixture.componentRef.setInput('text', 'MS-456');
      fixture.detectChanges();
      if (result === 'success') finish();
      else fail(new Error('Denied'));
      await pending;
      expect(page.message()).toBe('');
      expect(page.error()).toBe('');
      expect(page.busy()).toBe(false);
    },
  );
  it('clears feedback when text or label changes', async () => {
    const { fixture, page } = setup();
    await page.copy();
    fixture.componentRef.setInput('label', 'Trackingcode kopiëren');
    fixture.detectChanges();
    expect(page.message()).toBe('');
    await page.copy();
    fixture.componentRef.setInput('text', '3S-1');
    fixture.detectChanges();
    expect(page.message()).toBe('');
  });
  it('does not change feedback after the component is destroyed', async () => {
    let finish!: () => void;
    const promise = new Promise<void>((resolve) => (finish = resolve));
    const { fixture, page } = setup(vi.fn(() => promise));
    const pending = page.copy();
    fixture.destroy();
    finish();
    await pending;
    expect(page.message()).toBe('');
    expect(page.error()).toBe('');
  });
  it('does not copy an empty value', async () => {
    const { fixture, page, writeText } = setup();
    fixture.componentRef.setInput('text', '  ');
    fixture.detectChanges();
    await page.copy();
    expect(writeText).not.toHaveBeenCalled();
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBe(
      true,
    );
  });
});
