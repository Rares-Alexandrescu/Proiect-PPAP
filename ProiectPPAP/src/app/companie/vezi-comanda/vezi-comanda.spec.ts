import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziComanda } from './vezi-comanda';

describe('VeziComanda', () => {
  let component: VeziComanda;
  let fixture: ComponentFixture<VeziComanda>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziComanda],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziComanda);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
