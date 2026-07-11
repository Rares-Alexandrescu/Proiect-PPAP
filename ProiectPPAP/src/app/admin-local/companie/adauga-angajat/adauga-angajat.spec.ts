import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdaugaAngajat } from './adauga-angajat';

describe('AdaugaAngajat', () => {
  let component: AdaugaAngajat;
  let fixture: ComponentFixture<AdaugaAngajat>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdaugaAngajat],
    }).compileComponents();

    fixture = TestBed.createComponent(AdaugaAngajat);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
