import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SeteazaPretPiesaFurnizor } from './seteaza-pret-piesa-furnizor';

describe('SeteazaPretPiesaFurnizor', () => {
  let component: SeteazaPretPiesaFurnizor;
  let fixture: ComponentFixture<SeteazaPretPiesaFurnizor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SeteazaPretPiesaFurnizor],
    }).compileComponents();

    fixture = TestBed.createComponent(SeteazaPretPiesaFurnizor);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
