import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziLogisticaIesireDetaliat } from './vezi-logistica-iesire-detaliat';

describe('VeziLogisticaIesireDetaliat', () => {
  let component: VeziLogisticaIesireDetaliat;
  let fixture: ComponentFixture<VeziLogisticaIesireDetaliat>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziLogisticaIesireDetaliat],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziLogisticaIesireDetaliat);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
