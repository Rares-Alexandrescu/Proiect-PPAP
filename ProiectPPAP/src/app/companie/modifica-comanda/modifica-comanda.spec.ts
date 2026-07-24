import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModificaComanda } from './modifica-comanda';

describe('ModificaComanda', () => {
  let component: ModificaComanda;
  let fixture: ComponentFixture<ModificaComanda>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModificaComanda],
    }).compileComponents();

    fixture = TestBed.createComponent(ModificaComanda);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
