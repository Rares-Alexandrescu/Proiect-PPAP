import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziLogisticaIntrareDetaliat } from './vezi-logistica-intrare-detaliat';

describe('VeziLogisticaIntrareDetaliat', () => {
  let component: VeziLogisticaIntrareDetaliat;
  let fixture: ComponentFixture<VeziLogisticaIntrareDetaliat>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziLogisticaIntrareDetaliat],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziLogisticaIntrareDetaliat);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
