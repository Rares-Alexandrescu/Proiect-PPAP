import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ComenziCurente } from './comenzi-curente';

describe('ComenziCurente', () => {
  let component: ComenziCurente;
  let fixture: ComponentFixture<ComenziCurente>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ComenziCurente],
    }).compileComponents();

    fixture = TestBed.createComponent(ComenziCurente);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
