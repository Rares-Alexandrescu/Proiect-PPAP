import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VeziFurnizorii } from './vezi-furnizorii';

describe('VeziFurnizorii', () => {
  let component: VeziFurnizorii;
  let fixture: ComponentFixture<VeziFurnizorii>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VeziFurnizorii],
    }).compileComponents();

    fixture = TestBed.createComponent(VeziFurnizorii);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
