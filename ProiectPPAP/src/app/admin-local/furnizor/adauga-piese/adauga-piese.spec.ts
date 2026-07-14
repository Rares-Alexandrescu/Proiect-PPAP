import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdaugaPiese } from './adauga-piese';

describe('AdaugaPiese', () => {
  let component: AdaugaPiese;
  let fixture: ComponentFixture<AdaugaPiese>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdaugaPiese],
    }).compileComponents();

    fixture = TestBed.createComponent(AdaugaPiese);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
