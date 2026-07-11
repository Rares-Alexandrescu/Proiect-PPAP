import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziCompanie } from './vezi-companie';

describe('VeziCompanie', () => {
  let component: VeziCompanie;
  let fixture: ComponentFixture<VeziCompanie>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziCompanie],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziCompanie);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
