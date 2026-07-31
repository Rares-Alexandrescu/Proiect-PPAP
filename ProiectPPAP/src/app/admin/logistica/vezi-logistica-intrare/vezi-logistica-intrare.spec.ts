import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziLogisticaIntrare } from './vezi-logistica-intrare';

describe('VeziLogisticaIntrare', () => {
  let component: VeziLogisticaIntrare;
  let fixture: ComponentFixture<VeziLogisticaIntrare>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziLogisticaIntrare],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziLogisticaIntrare);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
