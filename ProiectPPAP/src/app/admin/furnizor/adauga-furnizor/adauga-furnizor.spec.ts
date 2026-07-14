import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdaugaFurnizor } from './adauga-furnizor';

describe('AdaugaFurnizor', () => {
  let component: AdaugaFurnizor;
  let fixture: ComponentFixture<AdaugaFurnizor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdaugaFurnizor],
    }).compileComponents();

    fixture = TestBed.createComponent(AdaugaFurnizor);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
