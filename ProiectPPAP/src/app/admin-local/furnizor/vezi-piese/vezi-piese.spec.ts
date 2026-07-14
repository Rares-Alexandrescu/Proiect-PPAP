import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziPiese } from './vezi-piese';

describe('VeziPiese', () => {
  let component: VeziPiese;
  let fixture: ComponentFixture<VeziPiese>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziPiese],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziPiese);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
