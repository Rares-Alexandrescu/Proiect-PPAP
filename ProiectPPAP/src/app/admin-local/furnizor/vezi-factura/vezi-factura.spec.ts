import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziFactura } from './vezi-factura';

describe('VeziFactura', () => {
  let component: VeziFactura;
  let fixture: ComponentFixture<VeziFactura>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziFactura],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziFactura);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
