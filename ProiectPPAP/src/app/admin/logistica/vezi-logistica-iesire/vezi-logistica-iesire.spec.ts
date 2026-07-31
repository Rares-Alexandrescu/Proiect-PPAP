import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziLogisticaIesire } from './vezi-logistica-iesire';

describe('VeziLogisticaIesire', () => {
  let component: VeziLogisticaIesire;
  let fixture: ComponentFixture<VeziLogisticaIesire>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziLogisticaIesire],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziLogisticaIesire);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
