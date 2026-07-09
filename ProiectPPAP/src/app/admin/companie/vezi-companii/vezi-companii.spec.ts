import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziCompanii } from './vezi-companii';

describe('VeziCompanii', () => {
  let component: VeziCompanii;
  let fixture: ComponentFixture<VeziCompanii>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziCompanii],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziCompanii);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
