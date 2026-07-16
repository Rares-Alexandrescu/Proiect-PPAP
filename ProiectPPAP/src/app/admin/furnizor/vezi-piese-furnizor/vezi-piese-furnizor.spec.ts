import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziPieseFurnizor } from './vezi-piese-furnizor';

describe('VeziPieseFurnizor', () => {
  let component: VeziPieseFurnizor;
  let fixture: ComponentFixture<VeziPieseFurnizor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziPieseFurnizor],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziPieseFurnizor);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
