import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NouaComanda } from './noua-comanda';

describe('NouaComanda', () => {
  let component: NouaComanda;
  let fixture: ComponentFixture<NouaComanda>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NouaComanda],
    }).compileComponents();

    fixture = TestBed.createComponent(NouaComanda);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
