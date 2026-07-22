import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdaugaPiesa } from './adauga-piesa';

describe('AdaugaPiesa', () => {
  let component: AdaugaPiesa;
  let fixture: ComponentFixture<AdaugaPiesa>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdaugaPiesa],
    }).compileComponents();

    fixture = TestBed.createComponent(AdaugaPiesa);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
