import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziFacturi } from './vezi-facturi';

describe('VeziFacturi', () => {
  let component: VeziFacturi;
  let fixture: ComponentFixture<VeziFacturi>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziFacturi],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziFacturi);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
